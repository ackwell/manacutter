namespace Manacutter.Common.Schema;

/// <summary>Representation of a column that references rows in one or more sheets.</summary>
public record ReferenceNode : SchemaNode {
	/// <summary>
	/// Ordered collection of targets for this node. The first active target sheet
	/// with a matching row ID is considered the resolved target.
	/// </summary>
	public IEnumerable<ReferenceTarget> Targets { get; init; } = new List<ReferenceTarget>();

	public override uint Size => 1;

	public override TReturn Accept<TContext, TReturn>(
		ISchemaVisitor<TContext, TReturn> visitor,
		TContext context
	) {
		return visitor.VisitReference(this, context);
	}
}

/// <summary>Potential target of a reference node.</summary>
public record ReferenceTarget(string Sheet) {
	/// <summary>Target sheet's string identifier.</summary>
	public string Sheet { get; init; } = Sheet;

	/// <summary>
	/// Name of the column in the target sheet whose value the parent reference node
	/// will contain the value of. If null, the value will reference the sheet's row ID.
	/// </summary>
	public string? Column { get; init; }

	/// <summary>
	/// Condition for this reference target. If non-null, the condition must be
	/// matched for this target to be considered active.
	/// </summary>
	public ReferenceCondition? Condition { get; init; }
}

/// <summary>Condition that must be matched for a reference target to be considered active.</summary>
public record ReferenceCondition(string Field, object Value) {
	/// <summary>Name of the column to check the value of.</summary>
	public string Field { get; init; } = Field;

	/// <summary>Value that checked column's value must match for this reference target to be considered active.</summary>
	public object Value { get; init; } = Value;
}
