using Manacutter.Definitions;
using Manacutter.Definitions.SaintCoinach;
using Manacutter.Definitions.Transformers;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection {
	public static class DefinitionServiceExtensions {
		public static IServiceCollection AddDefinitions(this IServiceCollection services, IConfiguration configuration) {
			services.Configure<SaintCoinachOptions>(configuration.GetSection(SaintCoinachOptions.Name));
			services.AddSingleton<IDefinitionProvider, SaintCoinachProvider>();

			services.AddHostedService<DefinitionHostedService>();

			// Middleware
			// TODO: Probably worth setting up automatic inclusion for these, maybe the providers too
			services.AddSingleton<ITransformer, CollapseSimple>();
			services.AddSingleton<ITransformer, Backfill>();
			// NOTE: This needs to be run after backfill always.
			services.AddSingleton<ITransformer, CleanReferences>();

			services.AddSingleton<IDefinitions, DefinitionsService>();

			return services;
		}
	}
}
