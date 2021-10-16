using Manacutter.Common.Schema;

namespace Manacutter.Readers;

public interface IReader {
	public IEnumerable<string> GetSheetNames();
	public ISheetReader? GetSheet(string sheetName);
}

public interface ISheetReader {
	public bool HasSubrows { get; }

	public uint ColumnCount { get; }

	public IColumnInfo? GetColumn(uint columnIndex);

	// TODO: Definitions throws, this is nullable. Need to solidy on a solution in one direction.
	public IRowReader? GetRow(uint rowId, uint? subRowId = null);
	public IEnumerable<IRowReader> EnumerateRows(uint? startRowId, uint? startSubRowId);
}

public interface IColumnInfo {
	public ScalarType Type { get; }
}

public interface IRowReader {
	public uint RowID { get; }
	public uint SubRowID { get; }

	// TODO: Should we have an explicit output type?
	public object ReadColumn(uint columnIndex);
}
