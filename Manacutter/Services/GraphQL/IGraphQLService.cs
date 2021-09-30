using Manacutter.Definitions;
using System.Text.Json;

namespace Manacutter.Services.GraphQL;

// TODO: better name
public interface IGraphQLService {
	public IGraphQLSchema GetSchema(SheetsNode definitionProvider);
}

public interface IGraphQLSchema {
	public Task<string> Query(string query, JsonElement variables);
}
