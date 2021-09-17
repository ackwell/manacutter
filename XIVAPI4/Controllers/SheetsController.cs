using Lumina;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace XIVAPI4.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SheetsController : ControllerBase {
	private GameData lumina;

	public SheetsController(GameData lumina) {
		this.lumina = lumina;
	}

	[HttpGet]
	public Task<string> GetDefinition() {
		var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		if (assemblyLocation is null) { throw new Exception("Shit's fucked"); }
		return System.IO.File.ReadAllTextAsync(Path.Combine(assemblyLocation, "Controllers", "temp-action-def.json"));
	}

	[HttpGet("action")]
	public string GetActionTest() {
		var something = lumina.Excel.GetSheetRaw("Action");
		var rp = something?.GetRowParser(7518);
		return rp?.ReadColumnRaw(0).ToString() ?? "not found";
	}
}
