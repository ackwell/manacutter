using Microsoft.AspNetCore.Mvc;
using XIVAPI4.Services.Readers;
using XIVAPI4.Services.SheetDefinitions;

namespace XIVAPI4.Controllers;

[Route("[controller]")]
[ApiController]
public class SheetsController : ControllerBase {
	private readonly IReader reader;
	private readonly IEnumerable<ISheetDefinitionProvider> definitionProviders;

	public SheetsController(
		IReader reader,
		IEnumerable<ISheetDefinitionProvider> definitionProviders
	) {
		this.reader = reader;
		this.definitionProviders = definitionProviders;
	}

	[HttpGet]
	public IActionResult GetSheets() {
		return this.Ok(this.reader.GetSheetNames());
	}

	[HttpGet("{sheetName}/{rowId}")]
	public IActionResult GetRow(string sheetName, uint rowId) {
		var sheet = this.reader.GetSheet(sheetName);
		if (sheet is null) {
			return this.Problem($"Requested sheet \"{sheetName}\" could not be found.", statusCode: StatusCodes.Status400BadRequest);
		}

		var row = sheet.GetRow(rowId);
		if (row is null) {
			return this.Problem($"Sheet \"{sheetName}\" does not contain an entry for requested rowId {rowId}.", statusCode: StatusCodes.Status400BadRequest);
		}

		// TODO: lookup properly
		var definitionProvider = this.definitionProviders.First();
		//var sheetReader = definitionProvider.GetReader(sheetName);
		// TODO: this might be service territory
		var rootNode = definitionProvider.GetRootNode(sheetName);

		return this.Ok(row.Read(rootNode));

		//return this.Ok(sheetReader.Read(rowParser));
	}
}
