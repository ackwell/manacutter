namespace Manacutter.Types;

public abstract class DataNode {
	// TODO: Work out how this will interact with arrays and so on
	public uint Index { get; init; }
}

public class StructNode : DataNode {
	public IDictionary<string, DataNode> Fields { get; }

	public StructNode(
		IDictionary<string, DataNode> fields
	) {
		this.Fields = fields;
	}
}

public class ScalarNode : DataNode {
	public ScalarType Type { get; init; }
}

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
