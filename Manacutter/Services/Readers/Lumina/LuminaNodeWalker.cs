using Lumina.Data.Structs.Excel;
using Lumina.Excel;
using Manacutter.Common.Schema;

namespace Manacutter.Services.Readers.Lumina;

public class LuminaNodeWalker : SchemaWalker<SchemaWalkerContext, object>, IRowReader {
	private readonly RowParser rowParser;

	public LuminaNodeWalker(
		RowParser rowParser
	) {
		this.rowParser = rowParser;
	}

	public uint RowID => this.rowParser.Row;
	public uint SubRowID => this.rowParser.SubRow;

	public object Read(SchemaNode node, uint offset) {
		return this.Visit(node, new SchemaWalkerContext() { Offset = offset });
	}

	public override object VisitSheets(SheetsNode node, SchemaWalkerContext context) {
		// TODO: how do sheet definitions work in the context of reading a row? We might need to rethink how the reader interfaces function a bit, move the reader to the sheets level more akin to the gql structure
		throw new NotImplementedException();
	}

	public override object VisitStruct(StructNode node, SchemaWalkerContext context) {
		return this.WalkStruct(node, context);
	}

	public override object VisitArray(ArrayNode node, SchemaWalkerContext context) {
		var baseOffset = context.Offset;
		var elementWidth = node.Type.Size;

		var value = new List<object>();
		for (uint index = 0; index < node.Count; index++) {
			var elementOffset = index * elementWidth;
			value.Add(this.WalkArray(node, context with { Offset = baseOffset + elementOffset }));
		}

		return value;
	}

	public override object VisitScalar(ScalarNode node, SchemaWalkerContext context) {
		var index = context.Offset;
		var value = this.rowParser.ReadColumnRaw((int)index);
		var column = this.rowParser.Sheet.Columns[index];

		// TODO: Will probably need slightly more involved logic for SeString in the long run.
		if (column.Type == ExcelColumnDataType.String) {
			value = value.ToString()!;
		}

		return value;
	}
}
