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

	// TODO: the results of this function should probably be cached in some manner
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
					var content = tree
						.Target
						.Peel<Git.Blob>()
						.GetContentText();

					// TODO: This supports reading a stream - might be able to stream straight from git into this?
					using var document = JsonDocument.Parse(content);
					return ReadSheetDefinition(document.RootElement);
				}
			);

		return new SheetsNode(sheets);
	}

	// -----

	private SchemaNode ReadSheetDefinition(in JsonElement element) {
		var fields = new Dictionary<string, SchemaNode>();

		var definitions = element.GetProperty("definitions");
		foreach (var definition in definitions.EnumerateArray()) {
			var (node, name) = this.ReadPositionedDataDefinition(definition);
			fields.Add(name ?? $"Unnamed{node.Offset}", node);
		}

		return new StructNode(fields);
	}

	private (SchemaNode, string?) ReadPositionedDataDefinition(in JsonElement element) {
		var index = element.TryGetProperty("index", out var property)
			? property.GetUInt32()
			: 0;

		var (node, name) = this.ReadDataDefinition(element);
		return (node with { Offset = index }, name);
	}

	private (SchemaNode, string?) ReadDataDefinition(in JsonElement element) {
		var type = element.TryGetProperty("type", out var property)
			? property.GetString()
			: null;

		return type switch {
			null => this.ReadSingleDataDefinition(element),
			"group" => this.ReadGroupDataDefinition(element),
			"repeat" => this.ReadRepeatDataDefinition(element),
			_ => throw new NotImplementedException(type)
		};
	}

	private (SchemaNode, string?) ReadSingleDataDefinition(in JsonElement element) {
		var name = element.GetProperty("name").GetString();
		var converterExists = element.TryGetProperty("converter", out var converter);

		if (!converterExists) {
			return (new ScalarNode(), name);
		}

		var type = converter.TryGetProperty("type", out var property)
			? property.GetString()
			: null;

		var node = type switch {
			"color" => this.ReadColorConverter(converter),
			"generic" => this.ReadGenericReferenceConverter(converter),
			"icon" => this.ReadIconConverter(converter),
			"multiref" => this.ReadMultiReferenceConverter(converter),
			"link" => this.ReadSheetLinkConverter(converter),
			"tomestone" => this.ReadTomestoneOrItemReferenceConverter(converter),
			"complexlink" => this.ReadComplexLinkConverter(converter),
			_ => throw new NotImplementedException(type),
		};

		return (node, name);
	}

	private (SchemaNode, string?) ReadGroupDataDefinition(in JsonElement element) {
		var fields = new Dictionary<string, SchemaNode>();

		uint size = 0;
		var members = element.GetProperty("members");
		foreach (var member in members.EnumerateArray()) {
			var (childNode, childName) = this.ReadDataDefinition(member);
			fields.Add(
				childName ?? $"Unnamed{size++}",
				childNode with { Offset = size }
			);
			size += childNode.Size;
		}

		var node = new StructNode(fields);
		var lcs = fields.Keys.Aggregate(Helpers.LongestCommonSubsequence);
		var name = lcs != "" ? lcs : null;

		return (new StructNode(fields), name);
	}

	private (SchemaNode, string?) ReadRepeatDataDefinition(in JsonElement element) {
		var (childNode, childName) = this.ReadDataDefinition(element.GetProperty("definition"));
		return (
			new ArrayNode(childNode, element.GetProperty("count").GetUInt32()),
			childName
		);
	}

	private SchemaNode ReadColorConverter(in JsonElement element) {
		// TODO: ?
		return new ScalarNode();
	}

	private SchemaNode ReadGenericReferenceConverter(in JsonElement element) {
		// TODO: ?
		return new ScalarNode();
	}

	private SchemaNode ReadIconConverter(in JsonElement element) {
		// TODO: ?
		return new ScalarNode();
	}

	private SchemaNode ReadMultiReferenceConverter(in JsonElement element) {
		// TODO: Reference node
		// element["targets"] = array of target sheet names
		return new ScalarNode();
	}

	private SchemaNode ReadSheetLinkConverter(in JsonElement element) {
		// TODO: Reference node
		// element["target"] = target sheet name
		return new ScalarNode();
	}

	private SchemaNode ReadTomestoneOrItemReferenceConverter(in JsonElement element) {
		// TODO: ?
		return new ScalarNode();
	}

	private SchemaNode ReadComplexLinkConverter(in JsonElement element) {
		// TODO: Reference node.
		// Yikes.
		// https://github.com/xivapi/SaintCoinach/blob/111543bf399c709529237cfb25f77650ddb0126f/SaintCoinach/Ex/Relational/ValueConverters/ComplexLinkConverter.cs#L143
		return new ScalarNode();
	}
}
