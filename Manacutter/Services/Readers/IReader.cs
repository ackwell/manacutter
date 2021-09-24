using Manacutter.Types;

namespace Manacutter.Services.Readers;

public interface IReader {
	public IEnumerable<string> GetSheetNames();
	public ISheetReader? GetSheet(string sheetName);
}

public interface ISheetReader {
	public IColumnInfo GetColumn(uint columnIndex);

	// TODO: Subrow handling
	public IRowReader? GetRow(uint rowId);
}

public interface IColumnInfo {
	public ScalarType Type { get; }
}

public interface IRowReader {
	// TODO: Should we have an explicit output type?
	public object Read(DataNode node);
}
