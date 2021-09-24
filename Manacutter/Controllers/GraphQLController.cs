using Manacutter.Services.Definitions;
using Manacutter.Services.GraphQL;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Manacutter.Controllers;

// TODO: This should really be a custom middleware, not a controller.
//       Or should it? idfk. Testing, testing, 123.
[Route("[controller]")]
[ApiController]
public class GraphQLController : ControllerBase {
	private readonly IEnumerable<IDefinitionProvider> definitionProviders;
	private readonly IGraphQLService graphQL;

	public GraphQLController(
		IEnumerable<IDefinitionProvider> definitionProviders,
		IGraphQLService graphQL
	) {
		this.graphQL = graphQL;
		this.definitionProviders = definitionProviders;
	}

	[HttpPost]
	public async Task<IActionResult> Get([FromBody] GraphQLRequest request) {
		// TODO: this makes two places that need to do all this lookup stuff
		var definitionProvider = this.definitionProviders.First();

		var schema = this.graphQL.GetSchema(definitionProvider);

		var json = await schema.Query(request.Query, request.Variables);

		return this.Ok(json);
	}
}

// TODO: yikes
public class GraphQLRequest {
	public string Query { get; set; } = "";
	public JsonElement Variables { get; set; }
}
