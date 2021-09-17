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
		//var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		//if (assemblyLocation is null) { throw new Exception("Shit's fucked"); }
		//return System.IO.File.ReadAllTextAsync(Path.Combine(assemblyLocation, "Controllers", "temp-action-def.json"));
	}

	[HttpGet("{sheetName}/{rowId}")]
	public IActionResult GetRow(string sheetName, uint rowId) {
		var sheet = lumina.Excel.GetSheetRaw(sheetName);
		if (sheet is null) {
			return this.Problem($"Requested sheet \"{sheetName}\" could not be found.", statusCode: StatusCodes.Status400BadRequest);
		}

		var rowParser = sheet.GetRowParser(rowId);
		if (rowParser is null) {
			return this.Problem($"Sheet \"{sheetName}\" does not contain an entry for requested rowId {rowId}.", statusCode: StatusCodes.Status400BadRequest);
		}

		return this.Ok(rowParser.ReadColumnRaw(0).ToString() ?? "not found");
	}
}
