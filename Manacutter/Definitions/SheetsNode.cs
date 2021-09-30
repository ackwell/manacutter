namespace Manacutter.Definitions;

/// <summary>Representation of a group of sheet definitions.</summary>
public record SheetsNode : DefinitionNode {
	/// <summary>Mapping of sheet root nodes by their name.</summary>
	public IReadOnlyDictionary<string, DefinitionNode> Sheets { get; init; }

	public SheetsNode(
		IReadOnlyDictionary<string, DefinitionNode> sheets
	) {
		this.Sheets = sheets;
	}

	// TODO: Maybe this should throw? Sheets has no "true" size, it's a collection of sheets. I don't think aggregating sizes of sheets is meaningful information.
	public override uint Size => 0;

	public override TReturn Accept<TContext, TReturn>(
		IDefinitionVisitor<TContext, TReturn> visitor,
		TContext context
	) {
		return visitor.VisitSheets(this, context);
	}
}
