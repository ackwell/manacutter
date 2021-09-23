namespace XIVAPI4.Types;

public abstract class DataNode {
	// TODO: Work out how this will interact with arrays and so on
	public int Index { get; init; }
}

public class StructNode : DataNode {
	public IDictionary<string, DataNode> Fields { get; }

	public StructNode(
		IDictionary<string, DataNode> fields
	) {
		this.Fields = fields;
	}
}

public class ScalarNode : DataNode { }
