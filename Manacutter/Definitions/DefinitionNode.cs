namespace Manacutter.Definitions;

public abstract class DefinitionNode {
	/// <summary>Column offset within this node's parent.</summary>
	public uint Offset { get; init; } = 0;

	/// <summary>Size of this node, in columns.</summary>
	public abstract uint Size { get; }

	public abstract TReturn Accept<TContext, TReturn>(
		IDefinitionVisitor<TContext, TReturn> visitor,
		TContext context
	);
}
