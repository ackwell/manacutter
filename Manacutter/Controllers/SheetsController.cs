﻿using Manacutter.Definitions;
using Manacutter.Services.Definitions;
using Manacutter.Services.Readers;
using Microsoft.AspNetCore.Mvc;

namespace Manacutter.Controllers;

[Route("[controller]")]
[ApiController]
public class SheetsController : ControllerBase {
	private readonly IReader reader;
	private readonly DefinitionsService definitions;

	public SheetsController(
		IReader reader,
		DefinitionsService definitions
	) {
		this.reader = reader;
		this.definitions = definitions;
	}

	[HttpGet]
	public IActionResult GetSheets() {
		return this.Ok(this.reader.GetSheetNames());
	}

	[HttpGet("{sheetName}/{rowId}")]
	public IActionResult GetRow(string sheetName, uint rowId) {
		sheetName = sheetName.ToLowerInvariant();

		var sheet = this.reader.GetSheet(sheetName);
		if (sheet is null) {
			return this.Problem($"Requested sheet \"{sheetName}\" could not be found.", statusCode: StatusCodes.Status400BadRequest);
		}

		var row = sheet.GetRow(rowId);
		if (row is null) {
			return this.Problem($"Sheet \"{sheetName}\" does not contain an entry for requested rowId {rowId}.", statusCode: StatusCodes.Status400BadRequest);
		}

		// TODO: Pass provider/version properly
		var sheetsNode = this.definitions.GetSheets(null, null);
		// TODO: While this is technically _safe_, where does it live? Is it a StC specific thing, a general definitions thing, or explicit to REST?
		//       It feels pretty rest-specific, but genning a new dict every time is ehh. Then again >C#
		var sheets = new Dictionary<string, DefinitionNode>(sheetsNode.Sheets, StringComparer.OrdinalIgnoreCase);
		if (!sheets.TryGetValue(sheetName, out var rootNode)) {
			return this.Problem($"Could not resolve definition for sheet \"{sheet}\".");
		}

		// TODO: expose row/subrow ids
		return this.Ok(row.Read(rootNode, 0));
	}
}
