using Manacutter.Common.Schema;

namespace Manacutter.Definitions.Transformers;

internal record CleanReferencesContext : SchemaWalkerContext {
	public IReadOnlyDictionary<string, SchemaNode>? Sheets { get; init; }
}

internal class CleanReferences : TransformerWalker<CleanReferencesContext> {
	public override SchemaNode VisitSheets(SheetsNode node, CleanReferencesContext context) {
		return base.VisitSheets(node, context with { Sheets = node.Sheets });
	}

	public override SchemaNode VisitReference(ReferenceNode node, CleanReferencesContext context) {
		// Process the reference, then fetch a list of targets that point to actual sheets that exist.
		var newNode = (ReferenceNode)base.VisitReference(node, context);
		var targets = newNode.Targets
			.Where(target => context.Sheets?.ContainsKey(target.Sheet) ?? false);

		// If this reference has no existing targets, replace it with a scalar so we can still see the value - it's probably an error in the definition.
		if (targets.Count() == 0) {
			return new ScalarNode();
		}

		return newNode with { Targets = targets };
	}
}
