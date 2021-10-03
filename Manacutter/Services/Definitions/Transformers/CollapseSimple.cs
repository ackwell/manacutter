using Manacutter.Definitions;

namespace Manacutter.Services.Definitions.Transformers;

public record CollapseSimpleContext : DefinitionWalkerContext {
	public bool IsSheetRoot { get; init; }
}

public class CollapseSimple : TransformerWalker<CollapseSimpleContext> {
	public override DefinitionNode VisitSheets(SheetsNode node, CollapseSimpleContext context) {
		return base.VisitSheets(node, context with { IsSheetRoot = true });
	}

	public override DefinitionNode VisitStruct(StructNode node, CollapseSimpleContext context) {
		var nextContext = context with { IsSheetRoot = false };

		// A struct with one field is pointless and can be collapsed
		if (context.IsSheetRoot || node.Fields.Count != 1) {
			return base.VisitStruct(node, nextContext);
		}

		var child = node.Fields.First().Value;
		return this.Visit(child with { Offset = child.Offset + node.Offset }, nextContext);
	}

	public override DefinitionNode VisitArray(ArrayNode node, CollapseSimpleContext context) {
		// An array of 1 element is equivent to its child
		if (node.Count > 1) {
			return base.VisitArray(node, context);
		}

		var child = node.Type with { Offset = node.Type.Offset + node.Offset };
		return this.Visit(child, context);
	}
}
