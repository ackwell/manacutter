using Microsoft.Extensions.Options;
using System.Text.Json;
using Git = LibGit2Sharp;

namespace XIVAPI4.Services.SheetDefinitions.SaintCoinach;

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
	}

	public void Dispose() {
		this.repository?.Dispose();
	}

	// TODO: the results of this function should probably be cached in some manner
	public ISheetReader GetReader(string sheet) {
		// TODO: ref should probably come from controller in some manner.
		var sheetDefinition = this.GetDefinition(sheet, "HEAD");

		// TODO: Proper recursive handling
		var fields = new Dictionary<string, ISheetReader>();
		foreach (var column in sheetDefinition.Definitions) {
			fields.Add(column.Name ?? "TODO", new ScalarReader() {
				Index = column.Index,
			});
		}

		return new StructReader(fields);
	}

	private SheetDefinition GetDefinition(string sheet, string reference) {
		var commit = repository?
			.Lookup(reference, Git.ObjectType.Commit)?
			.Peel<Git.Commit>();

		if (commit is null) {
			throw new ArgumentException($"Commit reference {reference} could not be resolved.");
		}

		// TODO: This will have casing issues &c. Will probably want to cache sheet (lower) -> filename per ref.
		// TODO: If we go back far enough, the coinach definitions were one massive json file - do we give a shit?
		// TODO: Is it worth checking the name field in the json files or is it always the same? Check coinach src I guess.
		var content = commit
			[$"SaintCoinach/Definitions/{sheet}.json"]?
			.Target
			.Peel<Git.Blob>()
			// TODO: Probably should be using streams or something.
			.GetContentText();

		if (content is null) {
			throw new ArgumentException($"Could not find definition for sheet {sheet} at commit {reference}.");
		}

		var sheetDefinition = JsonSerializer.Deserialize<SheetDefinition>(
			content,
			new JsonSerializerOptions {
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			}
		);

		// TODO: What error should this even be idfk.
		if (sheetDefinition is null) {
			throw new Exception($"Could not deserialize sheet {sheet} at commit {reference}.");
		}

		return sheetDefinition;
	}
}
