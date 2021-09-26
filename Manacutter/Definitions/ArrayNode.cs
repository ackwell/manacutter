namespace Manacutter.Definitions;

public class ArrayNode : DefinitionNode {
	public DefinitionNode Type { get; }
	public uint Count { get; }

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
