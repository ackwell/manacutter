using Lumina;
using Lumina.Data.Structs.Excel;
using Lumina.Excel;
using Manacutter.Common.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace Manacutter.Readers.Lumina;

internal class LuminaReader : IReader {
	private readonly IServiceProvider serviceProvider;
	private readonly GameData lumina;

	public LuminaReader(
		IServiceProvider serviceProvider,
		GameData lumina
	) {
		this.serviceProvider = serviceProvider;
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

		return LuminaSheetReader.Create(this.serviceProvider, sheet);
	}
}

internal class LuminaSheetReader : ISheetReader {
	internal static LuminaSheetReader Create(IServiceProvider provider, ExcelSheetImpl sheet) {
		return ActivatorUtilities.CreateInstance<LuminaSheetReader>(provider, sheet);
	}

	private readonly IServiceProvider serviceProvider;
	private readonly ExcelSheetImpl sheet;

	public LuminaSheetReader(
		IServiceProvider serviceProvider,
		ExcelSheetImpl sheet
	) {
		this.serviceProvider = serviceProvider;
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

	// TODO: Tempted to say that this should check hassubrows instead, and always take a subRowId
	// Would potentially make top-level logic easier, and let a lot of consumers ignore subrows as a concept
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

		return LuminaNodeWalker.Create(this.serviceProvider, rowParser);
	}

	public IEnumerable<IRowReader> EnumerateRows(uint? startRowId, uint? startSubRowId) {
		foreach (var page in this.sheet.DataPages) {
			// If this page doesn't contain the starting row, skip it entirely.
			if (startRowId is not null && !page.RowData.ContainsKey(startRowId.Value)) {
				continue;
			}

			// Local row parser for subrow handling.
			var parser = new RowParser(this.sheet, page.File);

			foreach (var rowPtr in page.RowData.Values) {
				// If we're looking for a starting row, skip until we find it.
				if (startRowId is not null) {
					if (rowPtr.RowId != startRowId.Value) {
						continue;
					}

					startRowId = null;
				}

				// If this sheet doesn't have subrows, just yield the row and skip further handling.
				if (!this.HasSubrows) {
					yield return this.GetRow(rowPtr.RowId, null)!;
					continue;
				}

				// Yield through all the subrows of this row.
				parser.SeekToRow(rowPtr.RowId);
				for (uint index = startSubRowId ?? 0; index < parser.RowCount; index++) {
					yield return this.GetRow(rowPtr.RowId, index)!;
				}
				startSubRowId = null;
			}
		}
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
