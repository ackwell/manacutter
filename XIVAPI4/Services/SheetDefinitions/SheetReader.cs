using Lumina.Data.Structs.Excel;
using Lumina.Excel;

namespace XIVAPI4.Services.SheetDefinitions;

// TODO: I really don't know where all this should go tbh.
public interface ISheetReader {
	public object Read(RowParser rowParser);
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
}
