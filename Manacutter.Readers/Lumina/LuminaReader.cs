using Lumina;
using Lumina.Data.Structs.Excel;
using Lumina.Excel;
using Manacutter.Common.Schema;

namespace Manacutter.Readers.Lumina;

internal class LuminaReader : IReader {
	private readonly GameData lumina;

	public LuminaReader(
		GameData lumina
	) {
		this.lumina = lumina;
	}

	public IEnumerable<string> GetSheetNames() {
		return this.lumina.Excel.SheetNames;
	}

	public ISheetReader? GetSheet(string sheetName) {
		var sheet = this.lumina.Excel.GetSheetRaw(sheetName);
		if (sheet is null) {
			return null;
		}

		return new LuminaSheetReader(sheet);
	}
}

internal class LuminaSheetReader : ISheetReader {
	private readonly ExcelSheetImpl sheet;

	internal LuminaSheetReader(
		ExcelSheetImpl sheet
	) {
		this.sheet = sheet;
	}

	public bool HasSubrows => this.sheet.Header.Variant == ExcelVariant.Subrows;

	public uint ColumnCount => this.sheet.ColumnCount;

	public IColumnInfo? GetColumn(uint columnIndex) {
		if (columnIndex < 0 || columnIndex >= this.sheet.ColumnCount) {
			return null;
		}

		return new LuminaColumnInfo() {
			Definition = this.sheet.Columns[columnIndex]
		};
	}

	public IRowReader? GetRow(uint rowId, uint? subRowId) {
		RowParser? rowParser = null;
		try {
			rowParser = subRowId is null
				? this.sheet.GetRowParser(rowId)
				: this.sheet.GetRowParser(rowId, subRowId.Value);
		} catch (IndexOutOfRangeException) {
			// noop
		}

		if (rowParser is null) {
			return null;
		}

		return new LuminaNodeWalker(rowParser);
	}
}

internal class LuminaColumnInfo : IColumnInfo {
	internal ExcelColumnDefinition Definition { get; init; }

	public ScalarType Type {
		get {
			switch (this.Definition.Type) {
				case ExcelColumnDataType.String:
					return ScalarType.String;
				case ExcelColumnDataType.Bool:
				case ExcelColumnDataType.PackedBool0:
				case ExcelColumnDataType.PackedBool1:
				case ExcelColumnDataType.PackedBool2:
				case ExcelColumnDataType.PackedBool3:
				case ExcelColumnDataType.PackedBool4:
				case ExcelColumnDataType.PackedBool5:
				case ExcelColumnDataType.PackedBool6:
				case ExcelColumnDataType.PackedBool7:
					return ScalarType.Boolean;
				case ExcelColumnDataType.Int8:
					return ScalarType.Int8;
				case ExcelColumnDataType.UInt8:
					return ScalarType.UInt8;
				case ExcelColumnDataType.Int16:
					return ScalarType.Int16;
				case ExcelColumnDataType.UInt16:
					return ScalarType.UInt16;
				case ExcelColumnDataType.Int32:
					return ScalarType.Int32;
				case ExcelColumnDataType.UInt32:
					return ScalarType.UInt32;
				case ExcelColumnDataType.Int64:
					return ScalarType.Int64;
				case ExcelColumnDataType.UInt64:
					return ScalarType.UInt64;
				case ExcelColumnDataType.Float32:
					return ScalarType.Float;
				default:
					return ScalarType.Unknown;
			}
		}
	}
}
