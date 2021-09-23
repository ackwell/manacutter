using Lumina;
using Lumina.Excel;
using Lumina.Data.Structs.Excel;
using XIVAPI4.Types;

namespace XIVAPI4.Services.Readers.Lumina;

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

	public IRowReader? GetRow(uint rowId) {
		var rowParser = this.sheet.GetRowParser(rowId);
		if (rowParser is null) {
			return null;
		}

		return new LuminaRowReader(rowParser);
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
		var value = this.rowParser.ReadColumnRaw(node.Index);
		// TODO: Will probably need slightly more involved logic for SeString in the long run.
		if (this.rowParser.Sheet.Columns[node.Index].Type == ExcelColumnDataType.String) {
			value = value.ToString()!;
		}
		return value;
	}
}
