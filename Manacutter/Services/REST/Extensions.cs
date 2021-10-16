using Manacutter.Services.REST;

namespace Microsoft.Extensions.DependencyInjection {
	public static class RESTServiceExtensions {
		public static IServiceCollection AddREST(this IServiceCollection services) {
			services.AddSingleton<RESTBuilder>();

			return services;
		}
	}
}
