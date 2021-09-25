namespace Manacutter.Types;

public abstract class DataNode {
	/// <summary>Column offset within this node's parent.</summary>
	public uint Offset { get; init; } = 0;

	/// <summary>Size of this node, in columns.</summary>
	public abstract uint Size { get; }
}

public class StructNode : DataNode {
	public IDictionary<string, DataNode> Fields { get; }

	public override uint Size {
		get => this.Fields.Values.Aggregate(
			(uint)0,
			(size, node) => size + node.Size
		);
	}

	public StructNode(
		IDictionary<string, DataNode> fields
	) {
		this.Fields = fields;
	}
}

public class ArrayNode : DataNode {
	public DataNode Type { get; }
	public uint Count { get; }

	public override uint Size {
		get => this.Type.Size * this.Count;
	}

	public ArrayNode(
		DataNode type,
		uint length
	) {
		this.Type = type;
		this.Count = length;
	}
}

public class ScalarNode : DataNode {
	public ScalarType Type { get; init; } = ScalarType.Unknown;

	public override uint Size { get => 1; }
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
