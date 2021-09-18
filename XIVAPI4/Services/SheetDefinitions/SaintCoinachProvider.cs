using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace XIVAPI4.Services.SheetDefinitions;

public class SaintCoinachProvider : ISheetDefinitionProvider {
	public string Name => "saint-coinach";

	public ISheetDefinition GetDefinition(string sheet) {
		// TODO: Do this properly.
		var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		if (assemblyLocation is null) { throw new Exception("Shit's fucked"); }

		var path = Path.Combine(new[] {
			assemblyLocation,
			"Services",
			"SheetDefinitions",
			"temp-action-def.json"
		});

		var sheetDefinition = JsonSerializer.Deserialize<CoinachSheetDefinition>(
			File.ReadAllText(path),
			new JsonSerializerOptions {
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			}
		);

		// TODO: Null handling
		return sheetDefinition;
	}
}

// temp
#pragma warning disable CS0649, CS8618
class CoinachSheetDefinition : ISheetDefinition {
	public List<CoinachColumnDefinition> Definitions { get; set; }

	public IEnumerable<IColumnDefinition> Columns => this.Definitions;
}

class CoinachColumnDefinition : IColumnDefinition {
	[JsonPropertyName("index")]
	public uint? MaybeIndex { get; set; }
	[JsonIgnore]
	public uint Index => this.MaybeIndex ?? 0;

	public string Name { get; set; }
}
#pragma warning restore CS0649, CS8618
