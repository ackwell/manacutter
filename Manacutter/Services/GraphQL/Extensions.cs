using Manacutter.Services.GraphQL;
using Manacutter.Services.GraphQL.GraphQLDotNet;

namespace Microsoft.Extensions.DependencyInjection {
	public static class GraphQLServiceExtensions {
		public static IServiceCollection AddGraphQL(this IServiceCollection services) {
			services.AddSingleton<IGraphQLService, GraphQLDotNetService>();

			return services;
		}
	}
}
