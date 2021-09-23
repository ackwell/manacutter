using XIVAPI4.Types;

namespace XIVAPI4.Services.Readers;

public interface IReader {
	public IEnumerable<string> GetSheetNames();
	public ISheetReader? GetSheet(string sheetName);
}

public interface ISheetReader {
	// TODO: Subrow handling
	public IRowReader? GetRow(uint rowId);
}

public interface IRowReader {
	// TODO: Should we have an explicit output type?
	public object Read(SheetNode node);
}
