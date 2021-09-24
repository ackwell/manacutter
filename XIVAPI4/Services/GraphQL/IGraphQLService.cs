using System.Text.Json;
using XIVAPI4.Types;

namespace XIVAPI4.Services.GraphQL;

// TODO: better name
public interface IGraphQLService {
	// TODO: How does this work alongside multiple sheets, ppagination, etc?
	//       Maybe this should build a _full_ schema, including every sheet?
	// TODO: Should probably remove the sheet name given the above.
	public IGraphQLSchema BuildSchema(string sheetName, DataNode node);
}

public interface IGraphQLSchema {
	public Task<string> Query(string query, JsonElement variables);
}
