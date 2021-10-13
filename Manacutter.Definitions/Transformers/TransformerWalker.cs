using Manacutter.Common.Schema;

namespace Manacutter.Definitions.Transformers;

// TODO: This structure results in a seperate dfs for every middleware - can we somehow merge the middleware into a single dfs?
abstract internal class TransformerWalker<TContext>
	: SchemaWalker<TContext, SchemaNode>, ITransformer
	where TContext : SchemaWalkerContext, new() {

	public SheetsNode Transform(SheetsNode node) {
		var newNode = this.Visit(node, new TContext());
		if (newNode is not SheetsNode) {
			// TODO: Better error
			throw new Exception("Result of transformation walk is not a sheets node.");
		}
		return (SheetsNode)newNode;
	}

	public override SchemaNode VisitSheets(SheetsNode node, TContext context) {
		return this.VisitSheets(node, context);
	}

	// TODO: I really don't like this.
	public SchemaNode VisitSheets(
		SheetsNode node,
		TContext context,
		Func<TContext, string, SchemaNode, TContext>? contextTransform = null
	) {
		return node with { Sheets = this.WalkSheets(node, context, contextTransform) };
	}

	public override SchemaNode VisitStruct(StructNode node, TContext context) {
		return node with { Fields = this.WalkStruct(node, context) };
	}

	public override SchemaNode VisitArray(ArrayNode node, TContext context) {
		return node with { Type = this.WalkArray(node, context) };
	}

	public override SchemaNode VisitReference(ReferenceNode node, TContext context) {
		return node;
	}

	public override SchemaNode VisitScalar(ScalarNode node, TContext context) {
		return node;
	}
}

// For people who don't need to specify their own
abstract internal class TransformerWalker : TransformerWalker<SchemaWalkerContext> { }
