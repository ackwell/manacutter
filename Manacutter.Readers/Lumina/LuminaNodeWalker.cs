using Lumina.Data.Structs.Excel;
using Lumina.Excel;
using Manacutter.Common.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace Manacutter.Readers.Lumina;

internal class LuminaNodeWalker : SchemaWalker<SchemaWalkerContext, object>, IRowReader {
	internal static LuminaNodeWalker Create(IServiceProvider provider, RowParser parser) {
		return ActivatorUtilities.CreateInstance<LuminaNodeWalker>(provider, parser);
	}

	// TODO: this should realistically be pulling in explicitly the lumina reader to make sure it doesn't swap impl halfway through handling?
	//       then again, we might not actually need it in here at all
	private IReader reader;
	private readonly RowParser rowParser;

	public LuminaNodeWalker(
		IReader reader,
		RowParser rowParser
	) {
		this.reader = reader;
		this.rowParser = rowParser;
	}

	public uint RowID => this.rowParser.Row;
	public uint SubRowID => this.rowParser.SubRow;

	public object Read(SchemaNode node, uint offset) {
		return this.Visit(node, new SchemaWalkerContext() { Offset = offset });
	}

	public override object VisitSheets(SheetsNode node, SchemaWalkerContext context) {
		// TODO: how do sheet definitions work in the context of reading a row? We might need to rethink how the reader interfaces function a bit, move the reader to the sheets level more akin to the gql structure
		throw new NotImplementedException();
	}

	public override object VisitStruct(StructNode node, SchemaWalkerContext context) {
		return this.WalkStruct(node, context);
	}

	public override object VisitArray(ArrayNode node, SchemaWalkerContext context) {
		var baseOffset = context.Offset;
		var elementWidth = node.Type.Size;

		var value = new List<object>();
		for (uint index = 0; index < node.Count; index++) {
			var elementOffset = index * elementWidth;
			value.Add(this.WalkArray(node, context with { Offset = baseOffset + elementOffset }));
		}

		return value;
	}

	public override object VisitReference(ReferenceNode node, SchemaWalkerContext context) {
		// TODO: We should probably maintain a list of visited rows (maybe even non-scoped?) to prevent infinite recursion
		// TODO: Sanity check the column type - is there a single type that's _always_ used for reference IDs?
		//       Sounds like it's any numeric type, so check against bool,string,etc.
		var targetRowId = Convert.ToUInt32(this.rowParser.ReadColumnRaw((int)context.Offset));

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

	public override object VisitScalar(ScalarNode node, SchemaWalkerContext context) {
		var index = context.Offset;
		var value = this.rowParser.ReadColumnRaw((int)index);
		var column = this.rowParser.Sheet.Columns[index];

		// TODO: Will probably need slightly more involved logic for SeString in the long run.
		if (column.Type == ExcelColumnDataType.String) {
			value = value.ToString()!;
		}

		return value;
	}
}
