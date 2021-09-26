namespace Manacutter.Definitions;

public enum ScalarType {
	Unknown = 0,
	String,
	Boolean,
	Int8,
	UInt8,
	Int16,
	UInt16,
	Int32,
	UInt32,
	Int64,
	UInt64,
	Float,
}

public class ScalarNode : DefinitionNode {
	public ScalarType Type { get; init; } = ScalarType.Unknown;

	public override uint Size { get => 1; }

	public override TReturn Accept<TContext, TReturn>(
		IDefinitionVisitor<TContext, TReturn> visitor,
		TContext context
	) {
		return visitor.VisitScalar(this, context);
	}
}
