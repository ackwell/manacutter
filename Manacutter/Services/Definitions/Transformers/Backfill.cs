using Manacutter.Definitions;
using Manacutter.Services.Readers;

namespace Manacutter.Services.Definitions.Transformers;

public record BackfillContext : DefinitionWalkerContext {
	public ISheetReader? Sheet { get; init; }
}

public class Backfill : TransformerWalker<BackfillContext> {
	private readonly IReader reader;

	public Backfill(
		IReader reader
	) {
		this.reader = reader;
	}

	// TODO: Add unmapped columns?

	public override DefinitionNode VisitSheets(SheetsNode node, BackfillContext context) {
		return base.VisitSheets(node, context, (context, name, _) => context with {
			Sheet = this.reader.GetSheet(name),
		});
	}

	public override DefinitionNode VisitScalar(ScalarNode node, BackfillContext context) {
		if (node.Type != ScalarType.Unknown || context.Sheet is null) {
			return base.VisitScalar(node, context);
		}

		var column = context.Sheet.GetColumn(context.Offset);

		return base.VisitScalar(node with {
			Type = column?.Type ?? ScalarType.Unknown,
		}, context);
	}
}
