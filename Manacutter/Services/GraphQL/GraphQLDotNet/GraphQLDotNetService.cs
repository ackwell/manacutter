using GraphQL.Relay.Types;
using Manacutter.Common.Schema;
using Manacutter.Readers;

namespace Manacutter.Services.GraphQL.GraphQLDotNet;

public class GraphQLDotNetService : IGraphQLService {
	private readonly IReader reader;

	public GraphQLDotNetService(
		IReader reader
	) {
		this.reader = reader;
	}

	// TODO: Cache schemas or something
	public IGraphQLSchema GetSchema(SheetsNode sheetsNode) {
		var graphType = new QueryGraphType();

		var builder = new FieldBuilder(this.reader);
		var sheetsField = builder.Visit(sheetsNode, new FieldBuilderContext());
		graphType.AddField(sheetsField);

		return new GraphQLDotNetSchema(graphType);
	}
}
