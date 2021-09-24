using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using XIVAPI4.Services.GraphQL;
using XIVAPI4.Services.SheetDefinitions;

namespace XIVAPI4.Controllers;

// TODO: This should really be a custom middleware, not a controller.
//       Or should it? idfk. Testing, testing, 123.
[Route("[controller]")]
[ApiController]
public class GraphQLController : ControllerBase {
	private readonly IEnumerable<ISheetDefinitionProvider> definitionProviders;
	private readonly IGraphQLService graphQL;

	public GraphQLController(
		IEnumerable<ISheetDefinitionProvider> definitionProviders,
		IGraphQLService graphQL
	) {
		this.graphQL = graphQL;
		this.definitionProviders = definitionProviders;
	}

	[HttpPost]
	public async Task<IActionResult> Get([FromBody] GraphQLRequest request) {
		// TODO: this makes two places that need to do all this lookup stuff
		var definitionProvider = this.definitionProviders.First();
		var rootNode = definitionProvider.GetRootNode("Action");

		var foo = this.graphQL.BuildSchema("Action", rootNode);

		var bar = await foo.Query(request.Query, request.Variables);

		return this.Ok(bar);
	}
}

// TODO: yikes
public class GraphQLRequest {
	public string Query { get; set; } = "";
	public JsonElement Variables { get; set; }
}
