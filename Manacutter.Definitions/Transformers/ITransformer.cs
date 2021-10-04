using Manacutter.Common.Schema;

namespace Manacutter.Definitions.Transformers;

internal interface ITransformer {
	public SheetsNode Transform(SheetsNode node);
}
