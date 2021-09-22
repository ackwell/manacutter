using GraphQL;
using GraphQL.Types;
using GraphQL.SystemTextJson;
using Lumina;
using Microsoft.AspNetCore.Mvc;
using XIVAPI4.Services.SheetDefinitions;

namespace XIVAPI4.Controllers;

// TODO: This should really be a custom middleware, not a controller.
//       Or should it? idfk. Testing, testing, 123.
[Route("[controller]")]
[ApiController]
public class GraphQLController : ControllerBase {
	private readonly IEnumerable<ISheetDefinitionProvider> definitionProviders;
	private readonly GameData lumina;

	public GraphQLController(
		IEnumerable<ISheetDefinitionProvider> definitionProviders,
		GameData lumina
	) {
		this.definitionProviders = definitionProviders;
		this.lumina = lumina;
	}

	[HttpPost]
	public async Task<IActionResult> Get([FromBody] GraphQLRequest request) {
		// this makes two places that need to do all this lookup stuff
		var sheet = this.lumina.Excel.GetSheetRaw("Action");
		var definitionProvider = this.definitionProviders.First();
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
		});

		return this.Ok(json);
	}
}

// TODO: yikes
public class GraphQLRequest {
	public string Query { get; set; } = "";
}
