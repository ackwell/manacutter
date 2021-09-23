namespace XIVAPI4.Types;

public abstract class SheetNode {
	// TODO: Work out how this will interact with arrays and so on
	public int Index { get; init; }
}

public class StructNode : SheetNode {
	public IDictionary<string, SheetNode> Fields { get; }

	public StructNode(
		IDictionary<string, SheetNode> fields
	) {
		this.Fields = fields;
	}
}

public class ScalarNode : SheetNode { }
