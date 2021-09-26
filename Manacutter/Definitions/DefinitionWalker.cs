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

	// TODO: I'm not sure how happy I am about this optional param. Investigate alternatives.
	protected IDictionary<string, TReturn> WalkStruct(
		StructNode node,
		TContext context,
		Func<TContext, string, DefinitionNode, TContext>? contextTransform = null
	) {
		return node.Fields.ToDictionary(
			pair => pair.Key,
			pair => {
				var newContext = context with {
					Offset = context.Offset + pair.Value.Offset
				};
				if (contextTransform is not null) {
					newContext = contextTransform(newContext, pair.Key, pair.Value);
				}
				return this.Visit(pair.Value, newContext);
			}
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
