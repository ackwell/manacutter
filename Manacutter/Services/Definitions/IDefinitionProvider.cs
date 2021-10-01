using Manacutter.Definitions;

namespace Manacutter.Services.Definitions;

public interface IDefinitionProvider {
	public string Name { get; }

	public Task Initialize();

	public SheetsNode GetSheets(string? version);
}
