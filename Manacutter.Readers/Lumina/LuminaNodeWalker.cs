using Lumina.Data.Structs.Excel;
using Lumina.Excel;
using Manacutter.Common.Schema;

namespace Manacutter.Readers.Lumina;

internal class LuminaNodeWalker : SchemaWalker<SchemaWalkerContext, object>, IRowReader {
	private readonly RowParser rowParser;

	internal LuminaNodeWalker(
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
		throw new NotImplementedException();
	}

	public override object VisitStruct(StructNode node, SchemaWalkerContext context) {
		throw new NotImplementedException();
	}

	public override object VisitArray(ArrayNode node, SchemaWalkerContext context) {
		throw new NotImplementedException();
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
