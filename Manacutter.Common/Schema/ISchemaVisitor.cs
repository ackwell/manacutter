namespace Manacutter.Common.Schema;

public interface ISchemaVisitor<TContext, TReturn> {
	public TReturn VisitSheets(SheetsNode node, TContext context);
	public TReturn VisitStruct(StructNode node, TContext context);
	public TReturn VisitArray(ArrayNode node, TContext context);
	public TReturn VisitReference(ReferenceNode node, TContext context);
	public TReturn VisitScalar(ScalarNode node, TContext context);
}
