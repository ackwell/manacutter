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
	private readonly ExcelSheetImpl sheet;

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

		//return new LuminaRowReader(rowParser);
		return new LuminaNodeWalker(rowParser);
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
	private readonly RowParser rowParser;

	public LuminaRowReader(
		RowParser rowParser
	) {
		this.rowParser = rowParser;
	}

	public uint RowID { get => this.rowParser.Row; }

	public object Read(DataNode node, uint offset) {
		// TODO: this is going to be a pretty common structure. Possibly make it a mixin... somehow?
		//       doubly so given the offset logic
		return node switch {
			StructNode structNode => this.ReadStruct(structNode, offset),
			ArrayNode arrayNode => this.ReadArray(arrayNode, offset),
			ScalarNode scalarNode => this.ReadScalar(scalarNode, offset),
			// TODO: Can we avoid this to make the exhaustiveness checking compile time?
			_ => throw new NotImplementedException(),
		};
	}

	private object ReadStruct(StructNode node, uint offset) {
		return node.Fields.ToDictionary(
			pair => pair.Key,
			pair => this.Read(pair.Value, offset + node.Offset)
		);
	}

	private object ReadArray(ArrayNode node, uint offset) {
		var elementWidth = node.Type.Size;

		var value = new List<object>();
		for (uint index = 0; index < node.Count; index++) {
			var elementOffset = index * elementWidth;
			value.Add(this.Read(node.Type, offset + node.Offset + elementOffset));
		}

		return value;
	}

	private object ReadScalar(ScalarNode node, uint offset) {
		var index = offset + node.Offset;
		var value = this.rowParser.ReadColumnRaw((int)index);
		var column = this.rowParser.Sheet.Columns[index];

		// TODO: Will probably need slightly more involved logic for SeString in the long run.
		if (column.Type == ExcelColumnDataType.String) {
			value = value.ToString()!;
		}

		return value;
	}
}
