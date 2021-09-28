namespace Manacutter.Definitions;

/// <summary> Representation of a group of named nodes.</summary>
public class StructNode : DefinitionNode {
	/// <summary>Mapping of names to their associated node trees.</summary>
	public IDictionary<string, DefinitionNode> Fields { get; }

	public StructNode(
		IDictionary<string, DefinitionNode> fields
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
