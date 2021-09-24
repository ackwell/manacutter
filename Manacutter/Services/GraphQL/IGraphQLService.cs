using Manacutter.Services.Definitions;
using System.Text.Json;

namespace Manacutter.Services.GraphQL;

// TODO: better name
public interface IGraphQLService {
	public IGraphQLSchema GetSchema(IDefinitionProvider definitionProvider);
}

public interface IGraphQLSchema {
	public Task<string> Query(string query, JsonElement variables);
}
