using Manacutter.Services.Definitions;
using Manacutter.Services.Definitions.SaintCoinach;
using Manacutter.Services.Definitions.Transformers;

namespace Microsoft.Extensions.DependencyInjection {
	public static class DefinitionServiceExtensions {
		public static IServiceCollection AddDefinitions(this IServiceCollection services, IConfiguration configuration) {
			services.Configure<SaintCoinachOptions>(configuration.GetSection(SaintCoinachOptions.Name));
			services.AddSingleton<IDefinitionProvider, SaintCoinachProvider>();

			services.AddHostedService<DefinitionHostedService>();

			// Middleware
			// TODO: Probably worth setting up automatic inclusion for these, maybe the providers too
			services.AddSingleton<ITransformer, CollapseSimple>();

			services.AddSingleton<DefinitionsService>();

			return services;
		}
	}
}
