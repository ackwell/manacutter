using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace XIVAPI4.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SheetsController : ControllerBase {
	[HttpGet]
	public Task<string> Get() {
		var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		if (assemblyLocation is null) { throw new Exception("Shit's fucked"); }
		return System.IO.File.ReadAllTextAsync(Path.Combine(assemblyLocation, "Controllers", "temp-action-def.json"));
	}
}
