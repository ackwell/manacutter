using Manacutter.Definitions;

namespace Manacutter.Services.Definitions.Middleware;

// TODO: look at what other transforms xivapi is currently doing. make sure the middleware interface covers what we need
//     - looks like a bunch of cross-sheet stuff, which isn't feasible under a single-walker setup (is it?)
// TODO: does this need to keep the generic on the context type? probably a good idea
// TODO: probably also an imiddleware with a much smaller surface (i.e. node->node apply) so we're not exposing a full walker interface every time
public class DefinitionMiddleware : DefinitionWalker<DefinitionWalkerContext, DefinitionNode> {
	public override DefinitionNode VisitSheets(SheetsNode node, DefinitionWalkerContext context) {
		return node with { Sheets = this.WalkSheets(node, context) };
	}

	public override DefinitionNode VisitStruct(StructNode node, DefinitionWalkerContext context) {
		return node with { Fields = this.WalkStruct(node, context) };
	}

	public override DefinitionNode VisitArray(ArrayNode node, DefinitionWalkerContext context) {
		return node with { Type = this.WalkArray(node, context) };
	}

	public override DefinitionNode VisitScalar(ScalarNode node, DefinitionWalkerContext context) {
		return node;
	}
}

public class CollapseSimple : DefinitionMiddleware {
	public override DefinitionNode VisitStruct(StructNode node, DefinitionWalkerContext context) {
		// TODO: How can we avoid this cast?
		var newNode = (StructNode)base.VisitStruct(node, context);

		if (newNode.Fields.Count != 1) {
			return newNode;
		}

		// A struct with one field is pointless and can be collapsed
		var child = newNode.Fields.First().Value;
		return child with { Offset = child.Offset + node.Offset };
	}

	public override DefinitionNode VisitArray(ArrayNode node, DefinitionWalkerContext context) {
		var newNode = (ArrayNode)base.VisitArray(node, context);

		// An array of 1 element is equivent to its child
		return newNode.Count > 1
			? newNode
			: newNode.Type with { Offset = newNode.Type.Offset + node.Offset };
	}
}
