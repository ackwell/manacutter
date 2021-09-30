using Manacutter.Services.Definitions;
using Manacutter.Services.Definitions.SaintCoinach;

namespace Microsoft.Extensions.DependencyInjection {
	public static class DefinitionServiceExtensions {
		public static IServiceCollection AddDefinitions(this IServiceCollection services, IConfiguration configuration) {
			services.Configure<SaintCoinachOptions>(configuration.GetSection(SaintCoinachOptions.Name));
			services.AddSingleton<IDefinitionProvider, SaintCoinachProvider>();

			services.AddHostedService<DefinitionHostedService>();

			services.AddSingleton<DefinitionsService>();

			return services;
		}
	}
}
