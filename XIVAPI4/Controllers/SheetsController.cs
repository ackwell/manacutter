using Lumina;
using Lumina.Data.Structs.Excel;
using Microsoft.AspNetCore.Mvc;
using XIVAPI4.Services.SheetDefinitions;

namespace XIVAPI4.Controllers;

[Route("[controller]")]
[ApiController]
public class SheetsController : ControllerBase {
	private readonly IEnumerable<ISheetDefinitionProvider> definitionProviders;
	private readonly GameData lumina;

	public SheetsController(
		IEnumerable<ISheetDefinitionProvider> definitionProviders,
		GameData lumina
	) {
		this.definitionProviders = definitionProviders;
		this.lumina = lumina;
	}

	[HttpGet]
	public IActionResult GetSheets() {
		return this.Ok(this.lumina.Excel.SheetNames);
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

		// TODO: lookup properly
		var definitionProvider = this.definitionProviders.First();
		var sheetReader = definitionProvider.GetReader(sheetName);

		return this.Ok(sheetReader.Read(rowParser));
	}
}
