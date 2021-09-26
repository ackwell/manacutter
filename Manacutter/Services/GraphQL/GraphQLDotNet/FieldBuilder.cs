using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using Manacutter.Services.Readers;
using Manacutter.Types;

namespace Manacutter.Services.GraphQL.GraphQLDotNet;

public record FieldBuilderContext : NodeWalkerContext {
	public ISheetReader Sheet { get; }

	public FieldBuilderContext(ISheetReader sheet) {
		this.Sheet = sheet;
	}
}

public class FieldBuilder : NodeWalker<FieldBuilderContext, FieldType> {
	public override FieldType VisitStruct(StructNode node, FieldBuilderContext context) {
		var type = new ObjectGraphType();

		foreach (var pair in this.WalkStruct(node, context)) {
			// TODO: lmao
			var name = pair.Key
				.Replace('{', '_').Replace('}', '_')
				.Replace('<', '_').Replace('>', '_');

			var fieldType = pair.Value;
			fieldType.Name = name;
			// TODO: this should be prefixed with parent names to prevent cross-sheet collisions
			// TODO: this.. really should be in the return, not modified afterwards
			var childType = fieldType.ResolvedType?.GetNamedType();
			if (childType is ObjectGraphType) {
				childType.Name = name;
			}
			type.AddField(fieldType);
		}

		return new FieldType() {
			ResolvedType = type,
			Resolver = new FuncFieldResolver<object>(context => {
				var executionContext = (ExecutionContext)context.Source!;
				// TODO: might be worth making a copy method for this
				return new ExecutionContext() {
					Sheet = executionContext.Sheet,
					Row = executionContext.Row,
					// TODO: this should be using the walker context offset
					Offset = executionContext.Offset + node.Offset,
				};
			})
		};
	}

	public override FieldType VisitArray(ArrayNode node, FieldBuilderContext context) {
		var fieldType = this.WalkArray(node, context);

		return new FieldType() {
			ResolvedType = new ListGraphType(fieldType.ResolvedType),
			Resolver = new FuncFieldResolver<object>(context => {
				var executionContext = (ExecutionContext)context.Source!;
				var baseOffset = executionContext.Offset + node.Offset;
				var elementWidth = node.Type.Size;

				var execContexts = context.ArrayPool.Rent<ExecutionContext>((int)node.Count);
				for (int index = 0; index < node.Count; index++) {
					var elementOffset = index * elementWidth;
					execContexts[index] = new ExecutionContext() {
						Sheet = executionContext.Sheet,
						Row = executionContext.Row,
						Offset = (uint)(baseOffset + elementOffset),
					};
				}

				return execContexts.Constrained((int)node.Count);
			}),
		};
	}

	public override FieldType VisitScalar(ScalarNode node, FieldBuilderContext context) {
		// If the node type wasn't provided by the definition, check the reader
		// TODO: If this needs doing in 2+ places, may be better off doing a one-off hydrate per sheet instance.
		var columnType = node.Type == ScalarType.Unknown
			? context.Sheet.GetColumn(context.Offset).Type
			: node.Type;

		// If it's an unknown type, we shortcut with an explicit unknown handler
		if (columnType == ScalarType.Unknown) {
			return new FieldType() {
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
			ResolvedType = graphType,
			Resolver = new FuncFieldResolver<object>(context => {
				var execContext = (ExecutionContext)context.Source!;
				// This is not including the offset, as it's added by the reader
				// TODO: The above seems janky, in a way. Think about it.
				return execContext.Row?.Read(node, execContext.Offset + node.Offset);
			}),
		};
	}
}
