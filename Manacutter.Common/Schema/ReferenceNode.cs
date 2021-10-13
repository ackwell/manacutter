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
public record ReferenceTarget {
	/// <summary>Target sheet's string identifier.</summary>
	public string Target { get; init; }

	public ReferenceTarget(string target) {
		this.Target = target;
	}
}

/// <summary>Potential target of a reference node, active only when a local field has a specific value.</summary>
public record ConditionalReferenceTarget : ReferenceTarget {
	/// <summary>Column offset, relative to parent reference node, of the column to check the value of.</summary>
	public int FieldOffset { get; init; }

	/// <summary>Value that checked column's value must match for this reference target to be considered active.</summary>
	public object? Value { get; init; } = null;

	public ConditionalReferenceTarget(string target) : base(target) { }
}
