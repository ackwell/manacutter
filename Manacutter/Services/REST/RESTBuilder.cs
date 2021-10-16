using Manacutter.Common.Schema;
using Manacutter.Readers;

namespace Manacutter.Services.REST;

// TODO: Work out a format for a "column filter" of sorts that can be a context/walkable system (list of things? idk). Doing that, we can remove a reasonable amount of the logic in the current sheets controller, and make this logic + walking considerably more self-contained.

public record RESTBuilderContext : SchemaWalkerContext {
	public IRowReader RowReader { get; init; }

	public RESTBuilderContext(IRowReader rowReader) : base() {
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

	public object Read(SchemaNode node, IRowReader rowReader) {
		return this.Visit(node, new RESTBuilderContext(rowReader));
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
		var targetRowId = Convert.ToUInt32(context.RowReader.ReadColumn(context.Offset));

		// Console.WriteLine(string.Join(',', node.Targets.Select(target => target.Target)));

		foreach (var target in node.Targets) {
			// TODO: conditional link checks here
			if (target is ConditionalReferenceTarget) {
				throw new NotImplementedException();
			}

			var sheet = this.reader.GetSheet(target.Target);
			if (sheet is null) { continue; }

			// TODO: handle subrows - probably want to enumeraterows.takewhile or something
			if (sheet.HasSubrows) {
				throw new NotImplementedException();
			}

			Console.WriteLine($"resolving {context.Offset} to {target.Target}");
			var row = sheet.GetRow(targetRowId);
			//row.Read()
			// TODO: we need the schema for all sheets at this point
			break;
		}

		return targetRowId;
	}

	public override object VisitScalar(ScalarNode node, RESTBuilderContext context) {
		return context.RowReader.ReadColumn(context.Offset);
	}
}
