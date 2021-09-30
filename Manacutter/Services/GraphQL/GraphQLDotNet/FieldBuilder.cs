using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using Manacutter.Definitions;
using Manacutter.Services.Readers;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Manacutter.Services.GraphQL.GraphQLDotNet;

public record FieldBuilderContext : DefinitionWalkerContext {
	public ISheetReader Sheet { get; }
	public ImmutableList<string> Path { get; init; } = ImmutableList<string>.Empty;

	public FieldBuilderContext(ISheetReader sheet) {
		this.Sheet = sheet;
	}
}

public class FieldBuilder : DefinitionWalker<FieldBuilderContext, FieldType> {
	private static string SanitizeName(string name) {
		// TODO: improve?
		return Regex.Replace(name, @"\W", "");
	}

	public override FieldType VisitSheets(SheetsNode node, FieldBuilderContext context) {
		throw new NotImplementedException();
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
			? context.Sheet.GetColumn(context.Offset).Type
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
