using Manacutter.Services.Definitions;
using Manacutter.Services.Readers;
using Microsoft.AspNetCore.Mvc;

namespace Manacutter.Controllers;

[Route("[controller]")]
[ApiController]
public class SheetsController : ControllerBase {
	private readonly IReader reader;
	private readonly IEnumerable<IDefinitionProvider> definitionProviders;

	public SheetsController(
		IReader reader,
		IEnumerable<IDefinitionProvider> definitionProviders
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
		// TODO: this might be service territory
		var rootNode = definitionProvider.GetRootNode(sheetName);

		// TODO: expose row/subrow ids
		return this.Ok(row.Read(rootNode, 0));
	}
}
