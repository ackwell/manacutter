using Lumina;
using Microsoft.AspNetCore.Mvc;

namespace XIVAPI4.Controllers;

[Route("[controller]")]
[ApiController]
public class SheetsController : ControllerBase {
	private GameData lumina;

	public SheetsController(GameData lumina) {
		this.lumina = lumina;
	}

	[HttpGet]
	public IActionResult GetSheets() {
		return this.Ok(this.lumina.Excel.SheetNames);
	}

	[HttpGet("{sheetName}/{rowId}")]
	public async Task<IActionResult> GetRow(string sheetName, uint rowId) {
		var sheet = lumina.Excel.GetSheetRaw(sheetName);
		if (sheet is null) {
			return this.Problem($"Requested sheet \"{sheetName}\" could not be found.", statusCode: StatusCodes.Status400BadRequest);
		}

		var rowParser = sheet.GetRowParser(rowId);
		if (rowParser is null) {
			return this.Problem($"Sheet \"{sheetName}\" does not contain an entry for requested rowId {rowId}.", statusCode: StatusCodes.Status400BadRequest);
		}

		// This is _very_ temp.
		var assemblyLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
		if (assemblyLocation is null) { throw new Exception("Shit's fucked"); }
		var rawDefinition = await System.IO.File.ReadAllTextAsync(Path.Combine(assemblyLocation, "Controllers", "temp-action-def.json"));
		var sheetDefinition = System.Text.Json.JsonSerializer.Deserialize<CoinachSheetDefinition>(
			rawDefinition,
			new System.Text.Json.JsonSerializerOptions {
				PropertyNamingPolicy=System.Text.Json.JsonNamingPolicy.CamelCase
			}
		);

		// TODO: Not sure how to do this "properly" in C#/ASP.
		var output = new Dictionary<string, object>();
		foreach (var rowDefinition in sheetDefinition.Definitions) {
			output.Add(rowDefinition.Name, rowParser.ReadColumnRaw((int?)rowDefinition.Index ?? 0).ToString() ?? "oops!");
		}

		return this.Ok(output);
	}
}

// temp
#pragma warning disable CS8618
class CoinachColumnDefinition {
	public uint? Index { get; set; }
	public string Name { get; set; }
}

class CoinachSheetDefinition {
	public string Sheet { get; set; }
	public string DefaultColumn { get; set; }
	public List<CoinachColumnDefinition> Definitions { get; set; }
}
#pragma warning restore CS8618
