using Manacutter.Definitions;

namespace Manacutter.Services.Definitions.Transformers;

// TODO: This structure results in a seperate dfs for every middleware - can we somehow merge the middleware into a single dfs?
abstract public class TransformerWalker<TContext>
	: DefinitionWalker<TContext, DefinitionNode>, ITransformer
	where TContext : DefinitionWalkerContext, new() {

	public SheetsNode Transform(SheetsNode node) {
		var newNode = this.Visit(node, new TContext());
		if (newNode is not SheetsNode) {
			// TODO: Better error
			throw new Exception("Result of transformation walk is not a sheets node.");
		}
		return (SheetsNode)newNode;
	}

	public override DefinitionNode VisitSheets(SheetsNode node, TContext context) {
		return this.VisitSheets(node, context);
	}

	// TODO: I really don't like this.
	public DefinitionNode VisitSheets(
		SheetsNode node,
		TContext context,
		Func<TContext, string, DefinitionNode, TContext>? contextTransform = null
	) {
		return node with { Sheets = this.WalkSheets(node, context, contextTransform) };
	}

	public override DefinitionNode VisitStruct(StructNode node, TContext context) {
		return node with { Fields = this.WalkStruct(node, context) };
	}

	public override DefinitionNode VisitArray(ArrayNode node, TContext context) {
		return node with { Type = this.WalkArray(node, context) };
	}

	public override DefinitionNode VisitScalar(ScalarNode node, TContext context) {
		return node;
	}
}

// For people who don't need to specify their own
abstract public class TransformerWalker : TransformerWalker<DefinitionWalkerContext> { }
