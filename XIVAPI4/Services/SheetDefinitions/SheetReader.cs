using GraphQL;
using GraphQL.Types;
using Lumina.Data.Structs.Excel;
using Lumina.Excel;

namespace XIVAPI4.Services.SheetDefinitions;

// TODO: I really don't know where all this should go tbh.
public interface ISheetReader {
	// TODO: I'd like to abstract lumina lookup logic, too
	public object Read(RowParser rowParser);

	// TODO: All GQL logic should be colocated, I'm not a fan of it cluttering up this file.
	public GraphType BuildGraph(ExcelSheetImpl sheet);
}

public class StructReader : ISheetReader {
	private IDictionary<string, ISheetReader> fields;

	public StructReader(
		IDictionary<string, ISheetReader> fields
	) {
		this.fields = fields;
	}

	public object Read(RowParser rowParser) {
		// TODO: What should I actually use here? Is Dict the right choice?
		var output = new Dictionary<string, object>();
		foreach (var field in fields) {
			output.Add(field.Key, field.Value.Read(rowParser));
		}
		return output;
	}

	public GraphType BuildGraph(ExcelSheetImpl sheet) {
		var type = new ObjectGraphType();
		foreach (var field in fields) {
			// TODO: lmao
			var name = field.Key
				.Replace('{', '_').Replace('}', '_')
				.Replace('<', '_').Replace('>', '_');

			type.Field(
				name,
				field.Value.BuildGraph(sheet),
				// TODO: read field from somewhere and decouple and shit and so forth
				// TODO: shouldn't be fetching the row parser at the struct level, as there may be substructs. rethink that.
				resolve: context => {
					// TODO: yikes
					var id = ((Dictionary<string, uint>)context.Source)["id"];
					return field.Value.Read(sheet.GetRowParser(id));
				}
			);
		}
		return type;
	}
}

public class ScalarReader : ISheetReader {
	// TODO: This doesn't hold up for array access &c. Think about alternatives. Might need to be a value on the Read() arg list
	public int? Index { get; set; }

	public object Read(RowParser rowParser) {
		var index = this.Index ?? 0;
		var value = rowParser.ReadColumnRaw(index);
		// TODO: Will probably need slightly more involved logic for SeString in the long run.
		if (rowParser.Sheet.Columns[index].Type == ExcelColumnDataType.String) {
			value = value.ToString()!;
		}
		return value;
	}

	public GraphType BuildGraph(ExcelSheetImpl sheet) {
		// same issue as other index thing. resolve.
		var index = this.Index ?? 0;
		switch (sheet.Columns[index].Type) {
			case ExcelColumnDataType.String:
				return new StringGraphType();
			case ExcelColumnDataType.Bool:
			case ExcelColumnDataType.PackedBool0:
			case ExcelColumnDataType.PackedBool1:
			case ExcelColumnDataType.PackedBool2:
			case ExcelColumnDataType.PackedBool3:
			case ExcelColumnDataType.PackedBool4:
			case ExcelColumnDataType.PackedBool5:
			case ExcelColumnDataType.PackedBool6:
			case ExcelColumnDataType.PackedBool7:
				return new BooleanGraphType();
			case ExcelColumnDataType.Int8:
				return new SByteGraphType();
			case ExcelColumnDataType.UInt8:
				return new ByteGraphType();
			case ExcelColumnDataType.Int16:
				return new ShortGraphType();
			case ExcelColumnDataType.UInt16:
				return new UShortGraphType();
			case ExcelColumnDataType.Int32:
				return new IntGraphType();
			case ExcelColumnDataType.UInt32:
				return new UIntGraphType();
			case ExcelColumnDataType.Int64:
				return new LongGraphType();
			case ExcelColumnDataType.UInt64:
				return new ULongGraphType();
			case ExcelColumnDataType.Float32:
				return new FloatGraphType();
			default:
				// TODO: dunno
				return new StringGraphType();
		}
	}
}
