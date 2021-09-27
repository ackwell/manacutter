using Manacutter.Definitions;
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
	public DefinitionNode GetRootNode(string sheet) {
		// TODO: ref should probably come from controller in some manner.
		var sheetDefinition = this.GetDefinition(sheet, "HEAD");

		// TODO: this is duped with group reader, possibly consolidate
		var fields = new Dictionary<string, DefinitionNode>();
		foreach (var column in sheetDefinition.Definitions) {
			var (node, name) = this.ParseDefinition(column, 0);
			fields.Add(
				column.Name ?? name ?? $"Unnamed{column.Index}",
				node
			);
		}

		return new StructNode(fields);
	}

	private (DefinitionNode, string?) ParseDefinition(DefinitionEntry definition, uint offset) {
		return definition.Type switch {
			null => this.ParseScalarDefinition(definition, offset),
			"repeat" => this.ParseRepeatDefinition(definition, offset),
			"group" => this.ParseGroupDefinition(definition, offset),
			_ => throw new ArgumentException($"Unknown definition type {definition.Type} at index {definition.Index}."),
		};
	}

	private (DefinitionNode, string?) ParseScalarDefinition(DefinitionEntry definition, uint offset) {
		var node = new ScalarNode() {
			Offset = definition.Index + offset,
		};

		return (node, definition.Name);
	}

	private (DefinitionNode, string?) ParseRepeatDefinition(DefinitionEntry definition, uint offset) {
		if (
			definition.Definition is null
			|| definition.Count is null
		) {
			throw new ArgumentException($"Invalid repeat definition.");
		}

		var (childNode, name) = this.ParseDefinition(definition.Definition, 0);
		var node = new ArrayNode(childNode, (uint)definition.Count) {
			Offset = definition.Index + offset,
		};
		return (node, name);
	}

	private (DefinitionNode, string?) ParseGroupDefinition(DefinitionEntry definition, uint offset) {
		if (definition.Members is null) {
			throw new ArgumentException($"Invalid group definition.");
		}

		var fields = new Dictionary<string, DefinitionNode>();
		uint size = 0;
		foreach (var member in definition.Members) {
			var (childNode, childName) = this.ParseDefinition(member, size);

			fields.Add(
				member.Name ?? childName ?? $"Unnamed{member.Index}",
				childNode
			);

			size += childNode.Size;
		}

		var node = new StructNode(fields) {
			Offset = definition.Index + offset,
		};
		var name = definition.Name;
		if (name is null) {
			var lcs = fields.Keys.Aggregate(GetLCS);
			name = lcs != "" ? lcs : null;
		}
		return (node, name);
	}

	// TODO: Move this somewhere more sensible lmao
	// TODO: Check if we need to look into optimisations e.g. suffix tree
	// Thanks, wikipedia
	private static string GetLCS(string a, string b) {
		// Initalise table
		int[,] table = new int[a.Length + 1, b.Length + 1];

		// LCS algo
		int i;
		int j;
		for (i = 1; i <= a.Length; i++) {
			for (j = 1; j <= b.Length; j++) {
				table[i, j] = a[i - 1] == b[j - 1]
					? table[i, j] = table[i - 1, j - 1] + 1
					: table[i - 1, j] > table[i, j - 1]
						? table[i - 1, j]
						: table[i, j - 1];
			}
		}

		// Backtrack the table into a string
		var output = "";
		i = a.Length;
		j = b.Length;
		while (i > 0 && j > 0) {
			if (a[i - 1] == b[j - 1]) {
				output = a[i - 1] + output;
				i--;
				j--;
			} else if (table[i - 1, j] > table[i, j - 1]) {
				i--;
			} else {
				j--;
			}
		}

		return output;
	}

	private SheetDefinition GetDefinition(string sheet, string reference) {
		var commit = repository?
			.Lookup(reference, Git.ObjectType.Commit)?
			.Peel<Git.Commit>();

		if (commit is null) {
			throw new ArgumentException($"Commit reference {reference} could not be resolved.");
		}

		// TODO: This should probably be cached on some basis
		// TODO: If we go back far enough, the coinach definitions were one massive json file - do we give a shit?
		// TODO: Is it worth checking the name field in the json files or is it always the same? Check coinach src I guess.
		var searchFor = $"{sheet.ToLowerInvariant()}.json";
		var content = commit
			["SaintCoinach/Definitions"]
			.Target
			.Peel<Git.Tree>()
			.FirstOrDefault(entry => entry.Name.ToLowerInvariant() == searchFor)?
			.Target
			.Peel<Git.Blob>()
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
