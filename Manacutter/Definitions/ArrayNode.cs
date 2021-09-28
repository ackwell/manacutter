namespace Manacutter.Definitions;

/// <summary>Representation of an array of one or more columns with repeated semantics.</summary>
public record ArrayNode : DefinitionNode {
	/// <summary>Node representing the type of column(s) in the array.</summary>
	public DefinitionNode Type { get; init; }
	/// <summary>Number of elements in the array.</summary>
	public uint Count { get; init; }

	public ArrayNode(
		DefinitionNode type,
		uint count
	) {
		this.Type = type;
		this.Count = count;
	}

	public override uint Size {
		get => this.Type.Size * this.Count;
	}

	public override TReturn Accept<TContext, TReturn>(
		IDefinitionVisitor<TContext, TReturn> visitor,
		TContext context
	) {
		return visitor.VisitArray(this, context);
	}
}
