using Manacutter.Definitions;

namespace Manacutter.Services.Definitions.Transformers;

public interface ITransformer {
	public DefinitionNode Transform(DefinitionNode node);
}
