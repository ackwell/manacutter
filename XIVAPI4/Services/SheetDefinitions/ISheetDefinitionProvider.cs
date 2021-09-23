using XIVAPI4.Types;

namespace XIVAPI4.Services.SheetDefinitions;

public interface ISheetDefinitionProvider {
	public string Name { get; }

	public Task Initialize();

	public DataNode GetRootNode(string sheet);

	[Obsolete] // TODO: finish migrating to the new node tree
	public ISheetReader GetReader(string sheet);
}
