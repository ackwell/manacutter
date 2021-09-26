using Manacutter.Definitions;

namespace Manacutter.Services.Readers;

public interface IReader {
	public IEnumerable<string> GetSheetNames();
	public ISheetReader? GetSheet(string sheetName);
}

public interface ISheetReader {
	public IColumnInfo GetColumn(uint columnIndex);

	// TODO: Subrow handling
	// TODO: Definitions throws, this is nullable. Need to solidy on a solution in one direction.
	public IRowReader? GetRow(uint rowId);
}

public interface IColumnInfo {
	public ScalarType Type { get; }
}

public interface IRowReader {
	public uint RowID { get; }
	// TODO: SubRowID

	// TODO: Should we have an explicit output type?
	public object Read(DefinitionNode node, uint offset);
}
