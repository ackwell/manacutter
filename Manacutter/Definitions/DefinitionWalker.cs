namespace Manacutter.Definitions;

public record DefinitionWalkerContext {
	public uint Offset { get; init; }
}

public abstract class DefinitionWalker<TContext, TReturn>
	: IDefinitionVisitor<TContext, TReturn>
	where TContext : DefinitionWalkerContext {

	public TReturn Visit(DefinitionNode node, TContext context) {
		return node.Accept(this, context);
	}

	public abstract TReturn VisitStruct(StructNode node, TContext context);

	protected IDictionary<string, TReturn> WalkStruct(StructNode node, TContext context) {
		return node.Fields.ToDictionary(
			pair => pair.Key,
			pair => this.Visit(pair.Value, context with {
				Offset = context.Offset + pair.Value.Offset
			})
		);
	}

	public abstract TReturn VisitArray(ArrayNode node, TContext context);

	protected TReturn WalkArray(ArrayNode node, TContext context) {
		return this.Visit(node.Type, context with {
			Offset = context.Offset + node.Type.Offset
		});
	}

	public abstract TReturn VisitScalar(ScalarNode node, TContext context);
}
