using Manacutter.Common.Schema;

namespace Manacutter.Definitions;

internal interface IDefinitionProvider {
	public string Name { get; }

	public Task Initialize();

	public SheetsNode GetSheets(string? version);
}
