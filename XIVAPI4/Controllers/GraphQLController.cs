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

		var graph = sheetReader.BuildGraph(sheet);
		var schema = new Schema() {
			// TODO: top level schema should be composed from multiple sheets with pagination and shit
			Query = (ObjectGraphType)graph
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
