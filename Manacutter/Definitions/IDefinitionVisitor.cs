namespace Manacutter.Definitions;

public interface IDefinitionVisitor<TContext, TReturn> {
	public TReturn VisitStruct(StructNode node, TContext context);
	public TReturn VisitArray(ArrayNode node, TContext context);
	public TReturn VisitScalar(ScalarNode node, TContext context);
}
