using Manacutter.Definitions;
using Manacutter.Services.Definitions.Middleware;

namespace Manacutter.Services.Definitions;

// Should this have an interface for mocking purposes &c?
public class DefinitionsService {
	private readonly IReadOnlyDictionary<string, IDefinitionProvider> definitions;

	public DefinitionsService(
		IEnumerable<IDefinitionProvider> definitionProviders
	) {
		this.definitions = definitionProviders.ToDictionary(provider => provider.Name);
	}

	public SheetsNode GetSheets(
		string? providerName,
		string? version
	) {
		// TODO: read default out of config or something
		providerName ??= "saint-coinach";

		if (!this.definitions.TryGetValue(providerName, out var provider)) {
			throw new ArgumentException($"Requested provider \"{providerName}\" could not be found.");
		}

		var sheetRoot = provider.GetSheets(version);

		// TODO: Some degree of caching, can apply at this level for the full set
		// TODO: Ideas like backfilling undefined fields/sheets can become a middleware concern

		// TODO: DI. and stuff.
		// TODO: the .Visit and context creation should be handled by the middleware - assuming we even stick to each middleware having it's own shit
		var collapseSimple = new CollapseSimple();
		var processed = collapseSimple.Visit(sheetRoot, new CollapseSimpleContext());
		return (SheetsNode)processed;
	}
}
