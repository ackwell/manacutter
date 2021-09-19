using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;
using Git = LibGit2Sharp;

namespace XIVAPI4.Services.SheetDefinitions;

public class SaintCoinachOptions {
	public const string Name = "SaintCoinach";

	public string Repository { get; set; } = "";
	public string Directory { get; set; } = $"./{Name}";
}

public class SaintCoinachProvider : ISheetDefinitionProvider, IDisposable {
	public string Name => "saint-coinach";

	private Git.Repository? repository;

	private readonly ILogger logger;
	private readonly SaintCoinachOptions options;
	private readonly IHostEnvironment hostEnvironment;

	public SaintCoinachProvider(
		ILogger<SaintCoinachProvider> logger,
		IOptions<SaintCoinachOptions> options,
		IHostEnvironment hostEnvironment
	) {
		this.logger = logger;
		this.options = options.Value;
		this.hostEnvironment = hostEnvironment;
	}

	// TODO: This should probably have some recurring task to fetch from origin.
	public async Task Initialize() {
		// Work out the path the repository is configured to.
		var repositoryPath = options.Directory;
		if (!Path.IsPathFullyQualified(repositoryPath)) {
			repositoryPath = Path.GetFullPath(repositoryPath, this.hostEnvironment.ContentRootPath);
		}
		logger.LogInformation($"Repository path: {repositoryPath}");

		// If no repository is available at the configured path, clone it down.
		if (!Directory.Exists(repositoryPath)) {
			logger.LogInformation("Cloning");
			await Task.Run(() => Git.Repository.Clone(
				options.Repository,
				repositoryPath,
				new Git.CloneOptions { IsBare = true }
			));
		}

		this.repository = new Git.Repository(repositoryPath);

		return;
	}

	public void Dispose() {
		this.repository?.Dispose();
	}

	public ISheetDefinition GetDefinition(string sheet) {
		// TODO: ref should probably come from controller in some manner.
		var definitionJson = this.GetDefinitionJSON(sheet, "HEAD");

		// TODO: this, properly.
		if (definitionJson is null) {
			throw new Exception($"couldn't find def");
		}

		var sheetDefinition = JsonSerializer.Deserialize<CoinachSheetDefinition>(
			definitionJson,
			new JsonSerializerOptions {
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			}
		);

		// TODO: Null handling
		return sheetDefinition;
	}

	// TOOD: Clean up this function a lot.
	private string? GetDefinitionJSON(string sheet, string reference) {
		var commit = repository?
			.Lookup(reference, Git.ObjectType.Commit)?
			.Peel<Git.Commit>();

		if (commit is null) {
			logger.LogWarning("commit missing");
			return null;
		}

		// TODO: This will have casing issues &c. Will probably want to cache sheet (lower) -> filename per ref.
		var treeEntry = commit[$"SaintCoinach/Definitions/{sheet}.json"];
		if (treeEntry is null) {
			logger.LogWarning("file missing");
			return null;
		}

		var blob = treeEntry.Target.Peel<Git.Blob>();
		// TODO: Probably should be using streams or something.
		var content = blob.GetContentText();
		return content;
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
