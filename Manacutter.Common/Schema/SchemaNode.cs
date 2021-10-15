namespace Manacutter.Common.Schema;

public abstract record SchemaNode {
	/// <summary>Size of this node, in columns.</summary>
	public abstract uint Size { get; }

	public abstract TReturn Accept<TContext, TReturn>(
		ISchemaVisitor<TContext, TReturn> visitor,
		TContext context
	);
}
