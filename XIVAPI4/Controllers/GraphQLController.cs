using GraphQL;
using GraphQL.Types;
using GraphQL.SystemTextJson;
using Microsoft.AspNetCore.Mvc;

namespace XIVAPI4.Controllers;

// TODO: This should really be a custom middleware, not a controller.
//       Or should it? idfk. Testing, testing, 123.
[Route("[controller]")]
[ApiController]
public class GraphQLController : ControllerBase {
	[HttpPost]
	public async Task<IActionResult> Get([FromBody] GraphQLRequest request) {
		var schema = Schema.For(@"
			type Query {
				hello: String
			}
		");

		var json = await schema.ExecuteAsync(_ => {
			_.Query = request?.Query;
			_.Root = new { Hello = "world" };
		});

		return this.Ok(json);
	}
}

// TODO: yikes
public class GraphQLRequest {
	public string Query { get; set; } = "";
}
