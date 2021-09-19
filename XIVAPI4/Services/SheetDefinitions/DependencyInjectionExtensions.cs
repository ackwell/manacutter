using XIVAPI4.Services.SheetDefinitions;
using XIVAPI4.Services.SheetDefinitions.SaintCoinach;

namespace Microsoft.Extensions.DependencyInjection;

public static class SheetDefinitionServiceExtensions {
	public static IServiceCollection AddSheetDefinitionProviders(this IServiceCollection services, IConfiguration configuration) {
		services.Configure<SaintCoinachOptions>(configuration.GetSection(SaintCoinachOptions.Name));
		services.AddSingleton<ISheetDefinitionProvider, SaintCoinachProvider>();

		services.AddHostedService<SheetDefinitionHostedService>();

		return services;
	}
}

internal class SheetDefinitionHostedService : IHostedService {
	private readonly IServiceProvider serviceProvider;
	public SheetDefinitionHostedService(IServiceProvider serviceProvider) {
		this.serviceProvider = serviceProvider;
	}

	public Task StartAsync(CancellationToken cancellationToken) {
		var providers = this.serviceProvider.GetRequiredService<IEnumerable<ISheetDefinitionProvider>>();
		return Task.WhenAll(providers.Select(provider => provider.Initialize()));
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
