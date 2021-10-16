﻿using Manacutter.Common.Schema;
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
		// TODO: We should probably maintain a list of visited rows (maybe even non-scoped?) to prevent infinite recursion
		// TODO: Sanity check the column type - is there a single type that's _always_ used for reference IDs?
		//       Sounds like it's any numeric type, so check against bool,string,etc.
		// Int as they occasionally use -1 for "no link"
		var temp = context.RowReader.ReadColumn(context.Offset);

		var targetRowId = Convert.ToInt32(temp);

		// TODO: what do we do for this case?
		if (targetRowId < 0) {
			return targetRowId;
		}

		// Console.WriteLine(string.Join(',', node.Targets.Select(target => target.Target)));

		// TODO: this should probably be the same logic as a no-targets-match result or something?
		// TODO: Configurable
		if (context.ReferenceDepth >= 1) {
			return targetRowId;
		}

		foreach (var target in node.Targets) {
			// TODO: conditional link checks here
			if (target is ConditionalReferenceTarget) {
				throw new NotImplementedException();
			}

			// Fetch the reader and definition for the sheet.
			var sheetReader = this.reader.GetSheet(target.Target);
			if (sheetReader is null) { continue; }

			// TODO: error check/trygetvalue?
			var sheetDefinition = context.Schema.Sheets[target.Target];

			// TODO: handle subrows - probably want to enumeraterows.takewhile or something
			if (sheetReader.HasSubrows) {
				throw new NotImplementedException();
			}

			var rowReader = sheetReader.GetRow((uint)targetRowId);

			// If there's no reader, the sheet doesn't contain this row. Pass on to the next sheet, if any, in the target list.
			if (rowReader is null) {
				continue;
			}

			Console.WriteLine($"resolving {context.Offset} to {target.Target}");

			// TODO: should this be .read or .visit? .visit will need to be careful with it's with{} or need a new()
			return this.Visit(sheetDefinition, context with {
				Offset = 0,
				RowReader = rowReader,
				ReferenceDepth = context.ReferenceDepth + 1
			});
		}

		return targetRowId;
	}

	public override object VisitScalar(ScalarNode node, RESTBuilderContext context) {
		return context.RowReader.ReadColumn(context.Offset);
	}
}
