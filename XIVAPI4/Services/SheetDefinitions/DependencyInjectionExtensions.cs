using XIVAPI4.Services.SheetDefinitions;

namespace Microsoft.Extensions.DependencyInjection;

public static class SheetDefinitionServiceExtensions {
	public static IServiceCollection AddSheetDefinitionProviders(this IServiceCollection services) {
		services.AddSingleton<ISheetDefinitionProvider, SaintCoinachProvider>();
		return services;
	}
}
