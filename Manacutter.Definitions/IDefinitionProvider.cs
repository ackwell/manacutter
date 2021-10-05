using Manacutter.Common.Schema;

namespace Manacutter.Definitions;

internal interface IDefinitionProvider {
	public string Name { get; }

	public Task Initialize();

	public string GetCanonicalVersion(string? version);

	public SheetsNode GetSheets(string version);
}
