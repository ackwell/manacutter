using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using Manacutter.Definitions;
using Manacutter.Services.Readers;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Manacutter.Services.GraphQL.GraphQLDotNet;

public record FieldBuilderContext : DefinitionWalkerContext {
	public ISheetReader? Sheet { get; init; }
	public ImmutableList<string> Path { get; init; } = ImmutableList<string>.Empty;
}

public class FieldBuilder : DefinitionWalker<FieldBuilderContext, FieldType> {
	private static string SanitizeName(string name) {
		// TODO: improve?
		name = Regex.Replace(name, @"\W", "");

		// Stolen from Adam because, and I quote, "kill me".
		// TODO: Better way?
		if (char.IsDigit(name[0])) {
			var index = name[0] - '0';
			// const?
			var lookup = new string[] { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine" };
			name = $"{lookup[index]}{name.Substring(1)}";
		}

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
			// TODO: how do i dedupe this? i mean realistically, it'd be good to remove the sheet from either the exec or build context
			Sheet = this.reader.GetSheet(name),
			Path = ImmutableList.Create(name),
		})) {
			var sheet = this.reader.GetSheet(name);
			if (sheet is null) { continue; }

			if (field.ResolvedType is not null) {
				field.ResolvedType.Name = name;
				this.AddIDFields(field.ResolvedType, sheet);
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

	private void AddIDFields(IGraphType graphType, ISheetReader sheet) {
		if (graphType is not ObjectGraphType) {
			return;
		}

		var objectGraphType = (ObjectGraphType)graphType;
		objectGraphType.Field("rowId", new UIntGraphType(), resolve: context => {
			var execContext = (ExecutionContext)context.Source!;
			return execContext.Row?.RowID;
		});

		if (sheet.HasSubrows) {
			objectGraphType.Field("subRowId", new UIntGraphType(), resolve: context => {
				var execContext = (ExecutionContext)context.Source!;
				return execContext.Row?.SubRowID;
			});
		}
	}

	public override FieldType VisitStruct(StructNode node, FieldBuilderContext context) {
		var type = new ObjectGraphType() {
			Name = string.Join('_', context.Path)
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

		// If the node type wasn't provided by the definition, check the reader
		// TODO: If this needs doing in 2+ places, may be better off doing a one-off hydrate per sheet instance.
		var columnType = node.Type == ScalarType.Unknown
			? context.Sheet?.GetColumn(context.Offset)?.Type ?? ScalarType.Unknown
			: node.Type;

		// If it's an unknown type, we shortcut with an explicit unknown handler
		if (columnType == ScalarType.Unknown) {
			return new FieldType() {
				Name = fieldName,
				ResolvedType = new StringGraphType(),
				Resolver = new FuncFieldResolver<object>(context => "UNKNOWN TYPE"),
			};
		}

		ScalarGraphType graphType = columnType switch {
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
