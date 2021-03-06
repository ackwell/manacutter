using Manacutter.Common.Schema;
using Manacutter.Readers;

namespace Manacutter.Definitions.Transformers;

internal record BackfillContext : SchemaWalkerContext {
	public bool IsSheetRoot { get; init; }
	public ISheetReader? Sheet { get; init; }
	public HashSet<uint>? DefinedColumns { get; init; }
}

// TODO: This is, due to the reader, reliant on the current game version. If we keep this as-is, it'll need to be computed, and cached, per game version.
internal class Backfill : TransformerWalker<BackfillContext> {
	private readonly IReader reader;

	public Backfill(
		IReader reader
	) {
		this.reader = reader;
	}

	public override SchemaNode VisitSheets(SheetsNode node, BackfillContext context) {
		// Walk all the known sheets from the definition
		var sheetsNode = (SheetsNode)base.VisitSheets(node, context with {
			IsSheetRoot = true,
		}, (context, name, _) => context with {
			Sheet = this.reader.GetSheet(name),
		});

		// Check for any sheets that we don't have a definition for, and build a backfill
		var missingSheetNames = this.reader.GetSheetNames()
			.Where(sheetName => !sheetsNode.Sheets.ContainsKey(sheetName))
			// TODO: This is incredibly naive, and is just a bandaid to remove custom/, quest/, etc sheets. Think up something more long-term reliable.
			.Where(sheetName => !sheetName.Contains('/'));

		var sheets = new Dictionary<string, SchemaNode>(sheetsNode.Sheets);
		foreach (var sheetName in missingSheetNames) {
			var structNode = new StructNode(new Dictionary<string, (uint, SchemaNode)>());
			var fsd = this.VisitStruct(structNode, context with {
				IsSheetRoot = true,
				Sheet = this.reader.GetSheet(sheetName),
			});
			sheets.Add(sheetName, fsd);
		}

		return sheetsNode with { Sheets = sheets };
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
		var fields = new Dictionary<string, (uint, SchemaNode)>(newNode.Fields);
		for (uint index = 0; index < expectedCount; index++) {
			if (columns.Contains(index)) { continue; }

			fields.Add($"Unknown{index}", (index, new ScalarNode() {
				Type = context.Sheet?.GetColumn(index)?.Type ?? ScalarType.Unknown,
			}));
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

	public override SchemaNode VisitReference(ReferenceNode node, BackfillContext context) {
		context.DefinedColumns?.Add(context.Offset);
		return node;
	}

	public override SchemaNode VisitScalar(ScalarNode node, BackfillContext context) {
		// Record this column as being defined
		context.DefinedColumns?.Add(context.Offset);

		if (node.Type != ScalarType.Unknown || context.Sheet is null) {
			return base.VisitScalar(node, context);
		}

		// Work out the column's type
		var column = context.Sheet.GetColumn(context.Offset);

		return base.VisitScalar(node with {
			Type = column?.Type ?? ScalarType.Unknown,
		}, context);
	}
}
