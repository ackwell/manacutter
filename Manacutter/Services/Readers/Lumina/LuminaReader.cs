using Lumina;
using Lumina.Data.Structs.Excel;
using Lumina.Excel;
using Manacutter.Types;

namespace Manacutter.Services.Readers.Lumina;

public class LuminaReader : IReader {
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

public class LuminaSheetReader : ISheetReader {
	private ExcelSheetImpl sheet;

	public LuminaSheetReader(
		ExcelSheetImpl sheet
	) {
		this.sheet = sheet;
	}

	public IColumnInfo GetColumn(uint columnIndex) {
		return new LuminaColumnInfo() {
			Definition = this.sheet.Columns[columnIndex]
		};
	}

	public IRowReader? GetRow(uint rowId) {
		var rowParser = this.sheet.GetRowParser(rowId);
		if (rowParser is null) {
			return null;
		}

		return new LuminaRowReader(rowParser);
	}
}

public class LuminaColumnInfo : IColumnInfo {
	public ExcelColumnDefinition Definition { get; init; }

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

public class LuminaRowReader : IRowReader {
	private RowParser rowParser;

	public LuminaRowReader(
		RowParser rowParser
	) {
		this.rowParser = rowParser;
	}

	public object Read(DataNode node) {
		// TODO: this is going to be a pretty common structure. Possibly make it a mixin... somehow?
		return node switch {
			StructNode structNode => this.ReadStruct(structNode),
			ScalarNode scalarNode => this.ReadScalar(scalarNode),
			// TODO: Can we avoid this to make the exhaustiveness checking compile time?
			_ => throw new NotImplementedException(),
		};
	}

	private object ReadStruct(StructNode node) {
		return node.Fields.ToDictionary(
			pair => pair.Key,
			pair => this.Read(pair.Value)
		);
	}

	private object ReadScalar(ScalarNode node) {
		var value = this.rowParser.ReadColumnRaw((int)node.Index);
		// TODO: Will probably need slightly more involved logic for SeString in the long run.
		if (this.rowParser.Sheet.Columns[node.Index].Type == ExcelColumnDataType.String) {
			value = value.ToString()!;
		}
		return value;
	}
}
