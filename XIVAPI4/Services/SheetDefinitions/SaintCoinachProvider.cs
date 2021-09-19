using Git = LibGit2Sharp;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace XIVAPI4.Services.SheetDefinitions;

public class SaintCoinachProvider : ISheetDefinitionProvider {
	public string Name => "saint-coinach";

	public SaintCoinachProvider(
		ILogger<SaintCoinachProvider> logger
	) {
		// I have no idea what I'm doing
		// TODO: Seriously, do this properly.
		// TODO: Clone should probably be done in a task or something.
		// TODO: Looks like this is instantiated lazily, which means this will chunk time off first request rather than boot. Fix. Probably should be a seperate init method.
		var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		if (assemblyLocation is null) { throw new Exception("Shit's fucked"); }
		logger.LogWarning("test");

		var repositoryPath = Path.Combine(assemblyLocation, "temp-coinach");
		if (!Directory.Exists(repositoryPath)) {
			logger.LogWarning($"test2 {repositoryPath}");
			Git.Repository.Clone(
				"https://github.com/xivapi/SaintCoinach.git",
				repositoryPath,
				new Git.CloneOptions { IsBare = true }
			);
			logger.LogWarning("test3");
		}

		// TODO: I should probably implement IDisposeable for this provider. Look into docs w/r/t disp on DI.
		using var repository = new Git.Repository(repositoryPath);
		var commit = repository
			.Lookup("594bc5391a640eee3be89d8bc6e22f30c460dfb8", Git.ObjectType.Commit)
			?.Peel<Git.Commit>();

		if (commit is null) {
			logger.LogWarning("commit missing");
			return;
		}

		// TODO: Will probably need to compute a list of sheet names -> file names ahead of time.
		var treeEntry = commit["SaintCoinach/Definitions/AirshipExplorationPoint.json"];
		if (treeEntry is null) {
			logger.LogWarning("file missing");
			return;
		}
		var blob = treeEntry.Target.Peel<Git.Blob>();
		// TODO: Probably should be using streams or something.
		var content = blob.GetContentText();
		logger.LogWarning(content);
	}

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
#pragma warning disable CS8618
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
#pragma warning restore CS8618
