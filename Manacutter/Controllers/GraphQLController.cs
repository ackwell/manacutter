using Manacutter.Definitions;
using Manacutter.Services.GraphQL;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Manacutter.Controllers;

// TODO: This should really be a custom middleware, not a controller.
//       Or should it? idfk. Testing, testing, 123.
[Route("[controller]")]
[ApiController]
public class GraphQLController : ControllerBase {
	private readonly IDefinitions definitions;
	private readonly IGraphQLService graphQL;

	public GraphQLController(
		IDefinitions definitions,
		IGraphQLService graphQL
	) {
		this.definitions = definitions;
		this.graphQL = graphQL;
	}

	[HttpPost]
	public async Task<IActionResult> Get([FromBody] GraphQLRequest request) {
		// TODO: Pass appropriate args
		var sheetsNode = this.definitions.GetSheets(null, null);

		var schema = this.graphQL.GetSchema(sheetsNode);

		var json = await schema.Query(request.Query, request.Variables);

		return this.Ok(json);
	}
}

// TODO: yikes
public class GraphQLRequest {
	public string Query { get; set; } = "";
	public JsonElement Variables { get; set; }
}
