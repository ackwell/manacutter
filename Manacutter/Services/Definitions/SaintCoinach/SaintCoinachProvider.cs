using Manacutter.Types;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Git = LibGit2Sharp;

namespace Manacutter.Services.Definitions.SaintCoinach;

public class SaintCoinachOptions {
	public const string Name = "SaintCoinach";

	public string Repository { get; set; } = "";
	public string Directory { get; set; } = $"./{Name}";
}

// TODO: this class has a weird mix of responsibilities between git logic and node building, should be split up.
public class SaintCoinachProvider : IDefinitionProvider, IDisposable {
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

	// TODO: the results of this function should probably be cached in some manner
	public DataNode GetRootNode(string sheet) {
		// TODO: ref should probably come from controller in some manner.
		var sheetDefinition = this.GetDefinition(sheet, "HEAD");

		// TODO: this is duped with group reader, possibly consolidate
		var fields = new Dictionary<string, DataNode>();
		foreach (var column in sheetDefinition.Definitions) {
			fields.Add(
				column.Name ?? $"Unnamed{column.Index}",
				this.ParseDefinition(column, 0)
			);
		}

		return new StructNode(fields);
	}

	private DataNode ParseDefinition(DefinitionEntry definition, uint offset) {
		return definition.Type switch {
			null => this.ParseScalarDefinition(definition, offset),
			"repeat" => this.ParseRepeatDefinition(definition, offset),
			"group" => this.ParseGroupDefinition(definition, offset),
			_ => throw new ArgumentException($"Unknown definition type {definition.Type} at index {definition.Index}."),
		};
	}

	private DataNode ParseScalarDefinition(DefinitionEntry definition, uint offset) {
		return new ScalarNode() {
			Offset = definition.Index + offset,
		};
	}

	private DataNode ParseRepeatDefinition(DefinitionEntry definition, uint offset) {
		if (
			definition.Definition is null
			|| definition.Count is null
		) {
			throw new ArgumentException($"Invalid repeat definition.");
		}

		return new ArrayNode(
			this.ParseDefinition(definition.Definition, 0),
			(uint)definition.Count
		) {
			Offset = definition.Index + offset,
		};
	}

	private DataNode ParseGroupDefinition(DefinitionEntry definition, uint offset) {
		if (definition.Members is null) {
			throw new ArgumentException($"Invalid group definition.");
		}

		var fields = new Dictionary<string, DataNode>();
		uint size = 0;
		foreach (var member in definition.Members) {
			var node = this.ParseDefinition(member, size);

			fields.Add(
				member.Name ?? $"Unnamed{member.Index}",
				node
			);

			size += node.Size;
		}
		return new StructNode(fields) {
			Offset = definition.Index + offset,
		};
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
