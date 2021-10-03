using Manacutter.Definitions;

namespace Manacutter.Services.Definitions.Transformers;

public interface ITransformer {
	public SheetsNode Transform(SheetsNode node);
}
