using GraphQL.Types;
using Manacutter.Definitions;
using Manacutter.Services.Readers;

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
		var graphType = new ObjectGraphType() { Name = "Query" };

		var builder = new FieldBuilder(this.reader);
		var sheetsField = builder.Visit(sheetsNode, new FieldBuilderContext());
		graphType.AddField(sheetsField);

		return new GraphQLDotNetSchema(graphType);
	}
}
