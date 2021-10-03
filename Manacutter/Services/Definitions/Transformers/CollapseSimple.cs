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
		// TODO: How can we avoid this cast?
		var newNode = (StructNode)base.VisitStruct(node, context with { IsSheetRoot = false});

		if (context.IsSheetRoot || newNode.Fields.Count != 1) {
			return newNode;
		}

		// A struct with one field is pointless and can be collapsed
		var child = newNode.Fields.First().Value;
		return child with { Offset = child.Offset + node.Offset };
	}

	public override DefinitionNode VisitArray(ArrayNode node, CollapseSimpleContext context) {
		var newNode = (ArrayNode)base.VisitArray(node, context);

		// An array of 1 element is equivent to its child
		return newNode.Count > 1
			? newNode
			: newNode.Type with { Offset = newNode.Type.Offset + node.Offset };
	}
}
