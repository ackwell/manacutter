using Manacutter.Common.Schema;
using Manacutter.Definitions.Transformers;
using Microsoft.Extensions.Caching.Memory;

namespace Manacutter.Definitions;

// Should this have an interface for mocking purposes &c?
internal class DefinitionsService : IDefinitions {
	private readonly IReadOnlyDictionary<string, IDefinitionProvider> definitions;
	private readonly IEnumerable<ITransformer> transformers;
	private readonly MemoryCache cache;
	private readonly TimeSpan cacheTTL;

	public DefinitionsService(
		IEnumerable<IDefinitionProvider> definitionProviders,
		IEnumerable<ITransformer> transformers
	) {
		this.definitions = definitionProviders.ToDictionary(provider => provider.Name);
		this.transformers = transformers;

		this.cache = new MemoryCache(new MemoryCacheOptions() { });
		// TODO: config. possible live updating? Currently 5m.
		this.cacheTTL = TimeSpan.Parse("00:05");
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

		var canonicalVersion = provider.GetCanonicalVersion(version);

		var pendingSheetRoot = this.cache.GetOrCreate((providerName, canonicalVersion), entry => {
			entry.SlidingExpiration = this.cacheTTL;
			return Task.Run(() => {
				var sheetRoot = provider.GetSheets(canonicalVersion);
				return this.transformers.Aggregate(
					sheetRoot,
					(node, transformer) => transformer.Transform(node)
				);
			});
		});

		return pendingSheetRoot.Result;
	}
}
