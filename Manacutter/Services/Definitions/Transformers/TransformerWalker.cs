using Manacutter.Definitions;

namespace Manacutter.Services.Definitions.Transformers;

// TODO: This structure results in a seperate dfs for every middleware - can we somehow merge the middleware into a single dfs?
abstract public class TransformerWalker<TContext>
	: DefinitionWalker<TContext, DefinitionNode>, ITransformer
	where TContext : DefinitionWalkerContext, new() {

	public DefinitionNode Transform(DefinitionNode node) {
		return this.Visit(node, new TContext());
	}

	public override DefinitionNode VisitSheets(SheetsNode node, TContext context) {
		return node with { Sheets = this.WalkSheets(node, context) };
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
