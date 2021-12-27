using Lumina.Data.Structs.Excel;
using Lumina.Excel;

namespace Manacutter.Readers.Lumina;

internal class LuminaRowReader : IRowReader {
	private readonly RowParser rowParser;

	internal LuminaRowReader(
		RowParser rowParser
	) {
		this.rowParser = rowParser;
	}

	public uint RowID => this.rowParser.RowId;
	public uint SubRowID => this.rowParser.SubRowId;

	public object ReadColumn(uint columnIndex) {
		var value = this.rowParser.ReadColumnRaw((int)columnIndex);
		var column = this.rowParser.Sheet.Columns[columnIndex];


		// TODO: Will probably need slightly more involved logic for SeString in the long run.
		if (column.Type == ExcelColumnDataType.String) {
			value = value?.ToString();
		}

		if (value is null) {
			throw new ArgumentOutOfRangeException();
		}

		return value;
	}
}
