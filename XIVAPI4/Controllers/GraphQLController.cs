using GraphQL;
using GraphQL.Types;
using GraphQL.SystemTextJson;
using Lumina;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using XIVAPI4.Services.GraphQL;
using XIVAPI4.Services.Readers;
using XIVAPI4.Services.SheetDefinitions;

namespace XIVAPI4.Controllers;

// TODO: This should really be a custom middleware, not a controller.
//       Or should it? idfk. Testing, testing, 123.
[Route("[controller]")]
[ApiController]
public class GraphQLController : ControllerBase {
	private readonly IEnumerable<ISheetDefinitionProvider> definitionProviders;
	private readonly IGraphQLService graphQL;
	private readonly IReader reader;
	private readonly GameData lumina;

	public GraphQLController(
		IEnumerable<ISheetDefinitionProvider> definitionProviders,
		IGraphQLService graphQL,
		IReader reader,
		GameData lumina
	) {
		this.reader = reader;
		this.graphQL = graphQL;
		this.definitionProviders = definitionProviders;
		this.lumina = lumina;
	}

	[HttpPost]
	public async Task<IActionResult> Get([FromBody] GraphQLRequest request) {
		var definitionProvider = this.definitionProviders.First();
		var rootNode = definitionProvider.GetRootNode("Action");

		var foo = this.graphQL.BuildSchema("Action", rootNode);

		var bar = await foo.Query(request.Query, request.Variables);

		return this.Ok(bar);


		// this makes two places that need to do all this lookup stuff
		var sheet = this.lumina.Excel.GetSheetRaw("Action");
		var sheetReader = definitionProvider.GetReader("Action");

		var actionGraph = sheetReader.BuildGraph(sheet);
		// TODO: this should not be in the controller.
		actionGraph.Name = "Action";
		var graph = new ObjectGraphType();
		graph.Field(
			"action",
			actionGraph,
			arguments: new QueryArguments(
				new QueryArgument<NonNullGraphType<UIntGraphType>>() { Name = "id" }
			),
			resolve: context => {
				var id = context.GetArgument<uint>("id");
				// this should be a shared class with the rest of the gql stuff. probably holds the row parser (or whatever we replace it with?)
				return new Dictionary<string, uint> { { "id", id } };
			}
		);

		var schema = new Schema() {
			// TODO: top level schema should be composed from multiple sheets with pagination and shit
			Query = graph
		};

		var json = await schema.ExecuteAsync(_ => {
			_.Query = request?.Query;
			_.Inputs = request?.Variables.ToInputs();
		});

		return this.Ok(json);
	}
}

// TODO: yikes
public class GraphQLRequest {
	public string Query { get; set; } = "";
	public JsonElement Variables { get; set; }
}
