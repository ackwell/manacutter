using Manacutter.Common.Schema;

namespace Manacutter.Definitions;

public interface IDefinitions {
	public SheetsNode GetSheets(string? providerName, string? version);
}
