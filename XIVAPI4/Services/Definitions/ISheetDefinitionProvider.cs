using XIVAPI4.Types;

namespace XIVAPI4.Services.Definitions;

public interface ISheetDefinitionProvider {
	public string Name { get; }

	public Task Initialize();

	public DataNode GetRootNode(string sheet);
}
