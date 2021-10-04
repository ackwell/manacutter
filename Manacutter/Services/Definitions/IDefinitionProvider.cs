using Manacutter.Common.Schema;

namespace Manacutter.Services.Definitions;

public interface IDefinitionProvider {
	public string Name { get; }

	public Task Initialize();

	public SheetsNode GetSheets(string? version);
}
