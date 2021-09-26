using Manacutter.Types;
using Lumina.Data.Structs.Excel;
using Lumina.Excel;

namespace Manacutter.Services.Readers.Lumina;

public class LuminaNodeWalker : NodeWalker<NodeWalkerContext, object>, IRowReader {
	private readonly RowParser rowParser;

	public uint RowID { get => this.rowParser.Row; }

	public LuminaNodeWalker(
		RowParser rowParser
	) {
		this.rowParser = rowParser;
	}

	public object Read(DataNode node, uint offset) {
		return node.Accept(this, new NodeWalkerContext() { Offset = offset });
	}

	public override object VisitStruct(StructNode node, NodeWalkerContext context) {
		return this.WalkStruct(node, context with { 
			Offset = context.Offset// + node.Offset
		});
	}

	public override object VisitArray(ArrayNode node, NodeWalkerContext context) {
		var baseOffset = context.Offset;// + node.Offset;
		var elementWidth = node.Type.Size;

		var value = new List<object>();
		for (uint index = 0; index < node.Count; index++) {
			var elementOffset = index * elementWidth;
			value.Add(this.WalkArray(node, context with { Offset = baseOffset + elementOffset }));
		}

		return value;
	}

	public override object VisitScalar(ScalarNode node, NodeWalkerContext context) {
		var index = context.Offset;// + node.Offset;
		var value = this.rowParser.ReadColumnRaw((int)index);
		var column = this.rowParser.Sheet.Columns[index];

		// TODO: Will probably need slightly more involved logic for SeString in the long run.
		if (column.Type == ExcelColumnDataType.String) {
			value = value.ToString()!;
		}

		return value;
	}
}
