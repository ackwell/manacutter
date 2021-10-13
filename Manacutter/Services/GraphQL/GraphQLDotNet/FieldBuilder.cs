using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using Manacutter.Common.Schema;
using Manacutter.Readers;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using GraphQL.Relay.Types;
using Humanizer;

namespace Manacutter.Services.GraphQL.GraphQLDotNet;

public record FieldBuilderContext : SchemaWalkerContext {
	public ImmutableList<string> Path { get; init; } = ImmutableList<string>.Empty;
}

public class FieldBuilder : SchemaWalker<FieldBuilderContext, FieldType> {
	private static readonly char IdSeparator = '/';
	private static string SanitizeName(string name) {
		// TODO: Some of this is very saint-specific, isolate?
		// Replace common symbols, then remove any remaining non-word symbols.
		name = name.Replace("%", "Percent");
		name = Regex.Replace(name, @"\W", "");

		// If there's a leading number, translate it to an english word representation.
		var leadingNumbers = name.TakeWhile(c => char.IsDigit(c)).ToArray();
		if (leadingNumbers.Length > 0) {
			var asWords = int.Parse(new string(leadingNumbers)).ToWords();
			name = $"{asWords}{name[leadingNumbers.Length..]}";
		}

		// We're splitting -> lowering before swapping to camel so that strings such as "AOZArrangement" become "aozArrangement" rather than "aOZArrangement".
		name = name.Humanize().ToLowerInvariant().Camelize();

		return name;
	}

	private readonly IReader reader;

	public FieldBuilder(
		IReader reader
	) : base() {
		this.reader = reader;
	}

	public override FieldType VisitSheets(SheetsNode node, FieldBuilderContext context) {
		// TODO: A bunch of stuff in this method assumes it's always the root - consider?
		var graphType = new ObjectGraphType() { Name = "Sheets" };

		foreach (var (name, field) in this.WalkSheets(node, context, (context, name, _) => context with {
			Path = ImmutableList.Create(SanitizeName(name)),
		})) {
			var sheet = this.reader.GetSheet(name);
			if (sheet is null) { continue; }

			if (field.ResolvedType is not null) {
				field.ResolvedType = this.AddIDFields(field.ResolvedType, sheet);
			}

			graphType.AddField(new SingularSheetFieldType(field, sheet));
			graphType.AddField(new PluralSheetFieldType(field, sheet));
		}

		return new FieldType() {
			Name = "Sheets",
			ResolvedType = new NonNullGraphType(graphType),
			Resolver = new FuncFieldResolver<object>(context => context.Source!)
		};
	}

	// TODO: Rename
	private IGraphType AddIDFields(IGraphType graphType, ISheetReader sheet) {
		if (graphType is not ObjectGraphType) {
			return graphType;
		}

		var objectGraphType = (ObjectGraphType)graphType;

		// TODO: This might be better off as a wrapper class akin to SingularSheetFieldType &c.
		// Build the core node graph type we'll use for the sheet.
		var nodeGraphType = new DefaultNodeGraphType<ExecutionContext, object?>(rowIdString => {
			// Resolve the ID string into the row ID, and subrow if this sheet supports them.
			uint? subRowId = null;
			if (sheet.HasSubrows) {
				// TODO: extract seperator into static
				var split = rowIdString.Split(IdSeparator);
				rowIdString = split[0];
				subRowId = uint.Parse(split[1]);
			}
			var rowId = uint.Parse(rowIdString);

			// Build the execution context for the node.
			var row = sheet.GetRow(rowId, subRowId);
			if (row is null) { return null; }
			return new ExecutionContext() { Row = row, GraphNodeName = graphType.Name };
		}) {
			Name = graphType.Name,
			IsTypeOf = input => {
				var execContext = (ExecutionContext)input;
				// The graph node name is only used when resolving node types - if it's not set, we arrived here through other means that we can assume are correct.
				if (execContext.GraphNodeName is null) { return true; }
				return graphType.Name == execContext.GraphNodeName;
			},
		};

		// Add the relay-compliant ID field. We're doing this manually rather than using .Id due to the latter's API not permitting the degree of customisation we need here.
		nodeGraphType.Field<NonNullGraphType<IdGraphType>>("id", resolve: context => {
			var row = context.Source!.Row;
			var id = $"{row?.RowID ?? 0}";
			if (sheet.HasSubrows) {
				id = $"{id}{IdSeparator}{row?.SubRowID ?? 0}";
			}
			return Node.ToGlobalId(context.ParentType.Name, id);
		});

		// Bring all the graph type's fields across to the new one.
		foreach (var field in objectGraphType.Fields) {
			// If there's an ID field, it'll conflict with the relay ID - prefix it with the type's name.
			if (field.Name.ToLowerInvariant() == "id") {
				field.Name = $"{graphType.Name}Id";
			}
			nodeGraphType.AddField(field);
		}

		// Add extra fields for the row's ID(s).
		nodeGraphType.Field<NonNullGraphType<UIntGraphType>>("rowId", resolve: context => {
			return context.Source?.Row?.RowID;
		});

		if (sheet.HasSubrows) {
			nodeGraphType.Field<NonNullGraphType<UIntGraphType>>("subRowId", resolve: context => {
				return context.Source?.Row?.SubRowID;
			});
		}

		return nodeGraphType;
	}

	public override FieldType VisitReference(ReferenceNode node, FieldBuilderContext context) {
		throw new NotImplementedException();
	}

	public override FieldType VisitStruct(StructNode node, FieldBuilderContext context) {
		var type = new ObjectGraphType() {
			Name = string.Join('_', context.Path.Select(part => part.Pascalize())),
		};

		foreach (var pair in this.WalkStruct(node, context, (context, name, _) => context with {
			Path = context.Path.Add(SanitizeName(name))
		})) {
			type.AddField(pair.Value);
		}

		return new FieldType() {
			Name = context.Path.Last(),
			ResolvedType = type,
			Resolver = new FuncFieldResolver<object>(context => {
				var executionContext = (ExecutionContext)context.Source!;
				// TODO: this should be using the walker context offset
				return executionContext with { Offset = executionContext.Offset + node.Offset };
			})
		};
	}

	public override FieldType VisitArray(ArrayNode node, FieldBuilderContext context) {
		var fieldType = this.WalkArray(node, context);

		return new FieldType() {
			Name = context.Path.Last(),
			ResolvedType = new ListGraphType(fieldType.ResolvedType),
			Resolver = new FuncFieldResolver<object>(context => {
				var executionContext = (ExecutionContext)context.Source!;
				var baseOffset = executionContext.Offset + node.Offset;
				var elementWidth = node.Type.Size;

				var execContexts = context.ArrayPool.Rent<ExecutionContext>((int)node.Count);
				for (int index = 0; index < node.Count; index++) {
					var elementOffset = index * elementWidth;
					execContexts[index] = executionContext with {
						Offset = (uint)(baseOffset + elementOffset),
					};
				}

				return execContexts.Constrained((int)node.Count);
			}),
		};
	}

	public override FieldType VisitScalar(ScalarNode node, FieldBuilderContext context) {
		var fieldName = context.Path.Last();

		// If it's an unknown type, we shortcut with an explicit unknown handler
		if (node.Type == ScalarType.Unknown) {
			return new FieldType() {
				Name = fieldName,
				ResolvedType = new StringGraphType(),
				Resolver = new FuncFieldResolver<object>(context => "UNKNOWN TYPE"),
			};
		}

		ScalarGraphType graphType = node.Type switch {
			ScalarType.String => new StringGraphType(),
			ScalarType.Boolean => new BooleanGraphType(),
			ScalarType.Int8 => new SByteGraphType(),
			ScalarType.UInt8 => new ByteGraphType(),
			ScalarType.Int16 => new ShortGraphType(),
			ScalarType.UInt16 => new UShortGraphType(),
			ScalarType.Int32 => new IntGraphType(),
			ScalarType.UInt32 => new UIntGraphType(),
			ScalarType.Int64 => new LongGraphType(),
			ScalarType.UInt64 => new ULongGraphType(),
			ScalarType.Float => new FloatGraphType(),
			_ => throw new NotImplementedException(),
		};

		return new FieldType() {
			Name = fieldName,
			ResolvedType = graphType,
			Resolver = new FuncFieldResolver<object>(context => {
				var execContext = (ExecutionContext)context.Source!;
				return execContext.Row?.Read(node, execContext.Offset + node.Offset);
			}),
		};
	}
}
