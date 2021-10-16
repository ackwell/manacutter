using Manacutter.Common.Schema;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Git = LibGit2Sharp;

namespace Manacutter.Definitions.SaintCoinach;

internal class SaintCoinachOptions {
	public const string Name = "SaintCoinach";

	public string Repository { get; set; } = "";
	public string Directory { get; set; } = $"./{Name}";
}

// TODO: this class has a weird mix of responsibilities between git logic and node building, should be split up.
internal class SaintCoinachProvider : IDefinitionProvider, IDisposable {
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
		this.logger.LogInformation($"Repository path: {repositoryPath}");

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

	public string GetCanonicalVersion(string? version) {
		// TODO: I should really consolidate null checking of the repo. Also like, a health init check or something.
		if (this.repository is null) {
			throw new ArgumentNullException("Repository not yet initialised");
		}

		// If no version is specified, resolve to the tip of HEAD's SHA
		if (version is null) {
			return this.repository.Head.Tip.Sha;
		}

		// Otherwise, resolve the ref to a git commit and grab it's SHA
		var commit = this.repository
			.Lookup(version, Git.ObjectType.Commit)?
			.Peel<Git.Commit>();

		if (commit is null) {
			throw new ArgumentException($"Commit reference {version} could not be resolved.");
		}

		return commit.Sha;
	}

	public SheetsNode GetSheets(string gitRef) {
		var commit = this.repository?
			.Lookup(gitRef, Git.ObjectType.Commit)?
			.Peel<Git.Commit>();

		if (commit is null) {
			throw new ArgumentException($"Commit reference {gitRef} could not be resolved.");
		}

		// TODO: If we go back far enough, the coinach definitions were one massive json file - do we give a shit?
		var sheets = commit
			["SaintCoinach/Definitions"]
			.Target
			.Peel<Git.Tree>()
			.Where(tree => tree.Name.EndsWith(".json"))
			.ToDictionary(
				tree => tree.Name.Substring(0, tree.Name.Length - 5),
				tree => {
					// TODO: this needs to be cleaned up a bit.
					var contentStream = tree
						.Target
						.Peel<Git.Blob>()
						.GetContentStream();

					using var document = JsonDocument.Parse(contentStream);
					return DefinitionReader.ReadSheetDefinition(document.RootElement);
				}
			);

		return new SheetsNode(sheets);
	}
}
