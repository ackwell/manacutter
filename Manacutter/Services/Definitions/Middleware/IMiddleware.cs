using Manacutter.Definitions;

namespace Manacutter.Services.Definitions.Middleware;

public interface IMiddleware {
	public DefinitionNode Transform(DefinitionNode node);
}
