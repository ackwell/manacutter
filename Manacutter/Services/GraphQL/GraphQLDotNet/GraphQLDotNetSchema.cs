using GraphQL.SystemTextJson;
using GraphQL.Types;
using Manacutter.Readers;
using System.Text.Json;

namespace Manacutter.Services.GraphQL.GraphQLDotNet;

public record ExecutionContext {
	public IRowReader? Row { get; set; }
	// TODO: This is only really _nessecary_ for array nodes - is there a better way to handle it?
	public uint Offset { get; set; } = 0;
}

public class GraphQLDotNetSchema : IGraphQLSchema {
	private readonly Schema schema;

	public GraphQLDotNetSchema(
		ObjectGraphType rootGraphType
	) {
		this.schema = new Schema() {
			Query = rootGraphType
		};
	}

	// TODO: think about the variables type a bit.
	public Task<string> Query(string query, JsonElement variables) {
		return this.schema.ExecuteAsync(options => {
			options.Query = query;
			options.Inputs = variables.ToInputs();
			options.Root = new ExecutionContext();
		});
	}
}
