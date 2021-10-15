using Manacutter.Common.Schema;

namespace Manacutter.Definitions.Transformers;

internal record CollapseSimpleContext : SchemaWalkerContext {
	public bool IsSheetRoot { get; init; }
}

internal class CollapseSimple : TransformerWalker<CollapseSimpleContext> {
	public override SchemaNode VisitSheets(SheetsNode node, CollapseSimpleContext context) {
		return base.VisitSheets(node, context with { IsSheetRoot = true });
	}

	public override SchemaNode VisitStruct(StructNode node, CollapseSimpleContext context) {
		var nextContext = context with { IsSheetRoot = false };

		// A struct with one field is pointless and can be collapsed
		if (context.IsSheetRoot || node.Fields.Count != 1) {
			return base.VisitStruct(node, nextContext);
		}

		// We can ignore the offset on single-field non-root structs, it'll always be zero. Technically.
		var child = node.Fields.First().Value.Node;
		return this.Visit(child, nextContext);
	}

	public override SchemaNode VisitArray(ArrayNode node, CollapseSimpleContext context) {
		// An array of 1 element is equivent to its child
		if (node.Count > 1) {
			return base.VisitArray(node, context);
		}

		return this.Visit(node.Type, context);
	}
}
