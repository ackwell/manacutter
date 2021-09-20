namespace XIVAPI4.Services.SheetDefinitions;

public interface ISheetDefinitionProvider {
	public string Name { get; }

	public Task Initialize();

	public ISheetReader GetReader(string sheet);
}
