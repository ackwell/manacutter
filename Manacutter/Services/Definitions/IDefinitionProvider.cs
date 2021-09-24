using Manacutter.Types;

namespace Manacutter.Services.Definitions;

public interface IDefinitionProvider {
	public string Name { get; }

	public Task Initialize();

	public DataNode GetRootNode(string sheet);
}
