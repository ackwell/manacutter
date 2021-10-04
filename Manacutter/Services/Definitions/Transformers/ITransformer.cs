using Manacutter.Common.Schema;

namespace Manacutter.Services.Definitions.Transformers;

public interface ITransformer {
	public SheetsNode Transform(SheetsNode node);
}
