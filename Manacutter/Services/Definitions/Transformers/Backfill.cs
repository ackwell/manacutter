using Manacutter.Common.Schema;
using Manacutter.Readers;

namespace Manacutter.Services.Definitions.Transformers;

public record BackfillContext : SchemaWalkerContext {
	public bool IsSheetRoot { get; init; }
	public ISheetReader? Sheet { get; init; }
	public HashSet<uint>? DefinedColumns { get; init; }
}

public class Backfill : TransformerWalker<BackfillContext> {
	private readonly IReader reader;

	public Backfill(
		IReader reader
	) {
		this.reader = reader;
	}

	public override SchemaNode VisitSheets(SheetsNode node, BackfillContext context) {
		return base.VisitSheets(node, context with {
			IsSheetRoot = true,
		}, (context, name, _) => context with {
			Sheet = this.reader.GetSheet(name),
		});
	}

	public override SchemaNode VisitStruct(StructNode node, BackfillContext context) {
		var newContext = context with { IsSheetRoot = false };

		// We only want to do extra processing for the root sheet struct
		if (!context.IsSheetRoot) {
			return base.VisitStruct(node, newContext);
		}

		// Collect IDs consumed by this sheet's definition
		var columns = new HashSet<uint>();
		var newNode = (StructNode)base.VisitStruct(node, newContext with {
			DefinedColumns = columns,
		});

		// If the definition accounts for every column, shortcut
		var expectedCount = context.Sheet?.ColumnCount ?? 0;
		if (columns.Count >= expectedCount) {
			return newNode;
		}

		// Find any column indexes not accounted for and add nodes for them
		var fields = new Dictionary<string, SchemaNode>(newNode.Fields);
		for (uint index = 0; index < expectedCount; index++) {
			if (columns.Contains(index)) { continue; }

			fields.Add($"Unknown{index}", new ScalarNode() {
				Offset = index,
				Type = context.Sheet?.GetColumn(index)?.Type ?? ScalarType.Unknown,
			});
		}

		return newNode with { Fields = fields };
	}

	public override SchemaNode VisitArray(ArrayNode node, BackfillContext context) {
		// Collect IDs consumed by the nodes in the array
		var arrayColumns = new HashSet<uint>();
		var newNode = (ArrayNode)base.VisitArray(node, context with { DefinedColumns = arrayColumns });

		// For each collected ID, add an offset copy of the ID to the parent context for each element in this array
		var elementWidth = newNode.Type.Size;
		foreach (var column in arrayColumns) {
			for (var index = 0; index < newNode.Count; index++) {
				var elementIndex = column + (index * elementWidth);
				context.DefinedColumns?.Add((uint)elementIndex);
			}
		}

		return newNode;
	}

	public override SchemaNode VisitScalar(ScalarNode node, BackfillContext context) {
		// Record this column as being defined
		context.DefinedColumns?.Add(context.Offset);

		if (node.Type != ScalarType.Unknown || context.Sheet is null) {
			return base.VisitScalar(node, context);
		}

		var column = context.Sheet.GetColumn(context.Offset);

		return base.VisitScalar(node with {
			Type = column?.Type ?? ScalarType.Unknown,
		}, context);
	}
}
