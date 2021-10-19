using Manacutter.Common.Schema;
using Manacutter.Readers;

namespace Manacutter.Services.REST;

// TODO: Work out a format for a "column filter" of sorts that can be a context/walkable system (list of things? idk). Doing that, we can remove a reasonable amount of the logic in the current sheets controller, and make this logic + walking considerably more self-contained.

public record RESTBuilderContext : SchemaWalkerContext {
	public SheetsNode Schema { get; init; }
	public IRowReader RowReader { get; init; }
	public int ReferenceDepth { get; init; } = 0;

	public RESTBuilderContext(
		SheetsNode schema,
		IRowReader rowReader
	) : base() {
		this.Schema = schema;
		this.RowReader = rowReader;
	}
}

// TODO: Not convinced by this name at all
// TODO: Consider an interface if this becomes non-trivial i guess
public class RESTBuilder : SchemaWalker<RESTBuilderContext, object> {
	private readonly IReader reader;

	public RESTBuilder(
		IReader reader
	) {
		this.reader = reader;
	}

	// TODO: Ew, that's not a nice signature. Should be fixable via the column filter idea.
	public object Read(SchemaNode node, SheetsNode schema, IRowReader rowReader) {
		return this.Visit(node, new RESTBuilderContext(schema, rowReader));
	}

	public override object VisitSheets(SheetsNode node, RESTBuilderContext context) {
		throw new NotImplementedException();
	}

	public override object VisitStruct(StructNode node, RESTBuilderContext context) {
		return this.WalkStruct(node, context);
	}

	public override object VisitArray(ArrayNode node, RESTBuilderContext context) {
		var elementWidth = node.Type.Size;

		var value = new List<object>();
		for (uint index = 0; index < node.Count; index++) {
			value.Add(this.WalkArray(node, context with {
				Offset = context.Offset + (index * elementWidth)
			}));
		}

		return value;
	}

	public override object VisitReference(ReferenceNode node, RESTBuilderContext context) {
		// TODO: Sanity check the column type - can be any of the numeric types.
		// Using int as they occasionally use -1 for "no link".
		var targetRowId = Convert.ToInt32(context.RowReader.ReadColumn(context.Offset));

		var result = new ReferenceResult(targetRowId);

		// A target < 0 is used to signify that no link is active for this row.  We're
		// also limiting the recursion depth to prevent pulling in the entire game in
		// one request, hah.
		// TODO: Configurable reference depth limit
		if (targetRowId < 0 || context.ReferenceDepth >= 1) {
			return result;
		}

		// It's common for multiple target conditions to check the same field - cache
		// values by field name to avoid re-reading
		var conditionFieldCache = new Dictionary<string, uint>();

		// Check each of the reference's configured targets.
		foreach (var target in node.Targets) {
			// If there's a condition on the target, check it.
			if (target.Condition is not null) {
				var condition = target.Condition;

				// Try to get the value from cache.
				if (!conditionFieldCache.TryGetValue(condition.Field, out var value)) {
					value = Convert.ToUInt32(this.GetFieldValue(context, condition.Field));
					conditionFieldCache[condition.Field] = value;
				}

				// If it doesn't match, this target is inactive.
				if (value != condition.Value) {
					continue;
				}
			}

			// Fetch the reader and definition for the sheet.
			result.Sheet = target.Sheet;

			var sheetReader = this.reader.GetSheet(target.Sheet);
			if (sheetReader is null) { continue; }

			if (!context.Schema.Sheets.TryGetValue(target.Sheet, out var sheetDefinition)) {
				continue;
			}

			// TODO: non-id lookups. they'll probably conflict with subrow logic in some way
			if (target.Field is not null) {
				result.Key = target.Field;
				throw new NotImplementedException();
			}

			// If the sheet has subrows, we need to enumerate over the subrows on the requested row
			if (sheetReader.HasSubrows) {
				var subRows = sheetReader.EnumerateRows((uint)targetRowId, null)
					.TakeWhile(reader => reader.RowID == targetRowId)
					.Select(reader => this.Visit(sheetDefinition, context with {
						Offset = 0,
						RowReader = reader,
						ReferenceDepth = context.ReferenceDepth + 1
					}));

				// A matching subrow will always have at least a /0 entry - ergo, a count of 0 means that there's no match at all.
				if (!subRows.Any()) {
					continue;
				}

				result.Data = subRows;

				break;
			}

			var rowReader = sheetReader.GetRow((uint)targetRowId);

			// If there's no reader, the sheet doesn't contain this row. Pass on to the next sheet, if any, in the target list.
			if (rowReader is null) {
				continue;
			}

			// TODO: There's now two iterations on this - abstract?
			result.Data = this.Visit(sheetDefinition, context with {
				Offset = 0,
				RowReader = rowReader,
				ReferenceDepth = context.ReferenceDepth + 1
			});

			break;
		}

		return result;
	}

	public override object VisitScalar(ScalarNode node, RESTBuilderContext context) {
		return context.RowReader.ReadColumn(context.Offset);
	}

	private object? GetFieldValue(RESTBuilderContext context, string field) {
		// Find the closest parent with a matching field, and calculate the field's column offset
		var column = context.EnumerateAncestors()
			.Where(context => context.Node is StructNode node && node.Fields.ContainsKey(field))
			.Select(context => {
				var structNode = (StructNode)context.Node!;
				return context.Offset + structNode.Fields[field].Offset;
			})
			.FirstOrDefault(uint.MaxValue);

		if (column == uint.MaxValue) {
			// TODO: This... shouldn't happen. Should probably log a debug or soemthing.
			return null;
		}

		// Read in the value & return
		return context.RowReader.ReadColumn(column);
	}
}

public record ReferenceResult(int Value) {
	public string? Sheet { get; set; }
	public string? Key { get; set; }
	public int Value { get; init; } = Value;
	public object? Data { get; set; }
}
