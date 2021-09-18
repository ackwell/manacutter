namespace XIVAPI4.Services.SheetDefinitions;

public interface ISheetDefinitionProvider {
	public string Name { get; }

	public ISheetDefinition GetDefinition(string sheet);
}

// TODO: These should probably be moved to a seperate file I guess? Or not. Not sure.
// TODO: Expand these interfaces.
public interface ISheetDefinition {
	public IEnumerable<IColumnDefinition> Columns { get; }
}

public interface IColumnDefinition {
	public uint Index { get; }
	public string Name { get; }
}
