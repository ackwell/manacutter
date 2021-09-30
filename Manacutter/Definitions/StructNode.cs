namespace Manacutter.Definitions;

/// <summary> Representation of a group of named nodes.</summary>
public record StructNode : DefinitionNode {
	/// <summary>Mapping of names to their associated node trees.</summary>
	public IReadOnlyDictionary<string, DefinitionNode> Fields { get; init; }

	public StructNode(
		IReadOnlyDictionary<string, DefinitionNode> fields
	) {
		this.Fields = fields;
	}

	public override uint Size {
		get => this.Fields.Values.Aggregate(
			(uint)0,
			(size, node) => size + node.Size
		);
	}

	public override TReturn Accept<TContext, TReturn>(
		IDefinitionVisitor<TContext, TReturn> visitor,
		TContext context
	) {
		return visitor.VisitStruct(this, context);
	}
}
