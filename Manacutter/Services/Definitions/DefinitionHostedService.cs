namespace Manacutter.Services.Definitions;

public class DefinitionHostedService : IHostedService {
	private readonly IServiceProvider serviceProvider;
	public DefinitionHostedService(IServiceProvider serviceProvider) {
		this.serviceProvider = serviceProvider;
	}

	public Task StartAsync(CancellationToken cancellationToken) {
		var providers = this.serviceProvider.GetRequiredService<IEnumerable<IDefinitionProvider>>();
		return Task.WhenAll(providers.Select(provider => provider.Initialize()));
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
