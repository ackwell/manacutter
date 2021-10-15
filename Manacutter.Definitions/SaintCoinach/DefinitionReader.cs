using Manacutter.Common.Schema;
using System.Text.Json;

namespace Manacutter.Definitions.SaintCoinach;

internal static class DefinitionReader {
	internal static SchemaNode ReadSheetDefinition(in JsonElement element) {
		var fields = new Dictionary<string, SchemaNode>();

		var definitions = element.GetProperty("definitions");
		foreach (var definition in definitions.EnumerateArray()) {
			var (node, name) = ReadPositionedDataDefinition(definition);
			fields.Add(name ?? $"Unnamed{node.Offset}", node);
		}

		return new StructNode(fields);
	}

	private static (SchemaNode, string?) ReadPositionedDataDefinition(in JsonElement element) {
		var index = element.TryGetProperty("index", out var property)
			? property.GetUInt32()
			: 0;

		var (node, name) = ReadDataDefinition(element);
		return (node with { Offset = index }, name);
	}

	private static (SchemaNode, string?) ReadDataDefinition(in JsonElement element) {
		var type = element.TryGetProperty("type", out var property)
			? property.GetString()
			: null;

		return type switch {
			null => ReadSingleDataDefinition(element),
			"group" => ReadGroupDataDefinition(element),
			"repeat" => ReadRepeatDataDefinition(element),
			_ => throw new NotImplementedException(type)
		};
	}

	private static (SchemaNode, string?) ReadSingleDataDefinition(in JsonElement element) {
		var name = element.GetProperty("name").GetString();
		var converterExists = element.TryGetProperty("converter", out var converter);

		if (!converterExists) {
			return (new ScalarNode(), name);
		}

		var type = converter.TryGetProperty("type", out var property)
			? property.GetString()
			: null;

		var node = type switch {
			"color" => ReadColorConverter(converter),
			"generic" => ReadGenericReferenceConverter(converter),
			"icon" => ReadIconConverter(converter),
			"multiref" => ReadMultiReferenceConverter(converter),
			"link" => ReadSheetLinkConverter(converter),
			"tomestone" => ReadTomestoneOrItemReferenceConverter(converter),
			"complexlink" => ReadComplexLinkConverter(converter),
			_ => throw new NotImplementedException(type),
		};

		return (node, name);
	}

	private static (SchemaNode, string?) ReadGroupDataDefinition(in JsonElement element) {
		var fields = new Dictionary<string, SchemaNode>();

		uint size = 0;
		var members = element.GetProperty("members");
		foreach (var member in members.EnumerateArray()) {
			var (childNode, childName) = ReadDataDefinition(member);
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

	private static (SchemaNode, string?) ReadRepeatDataDefinition(in JsonElement element) {
		var (childNode, childName) = ReadDataDefinition(element.GetProperty("definition"));
		return (
			new ArrayNode(childNode, element.GetProperty("count").GetUInt32()),
			childName
		);
	}

	private static SchemaNode ReadColorConverter(in JsonElement element) {
		// TODO: ?
		return new ScalarNode();
	}

	private static SchemaNode ReadGenericReferenceConverter(in JsonElement element) {
		// TODO: ?
		return new ScalarNode();
	}

	private static SchemaNode ReadIconConverter(in JsonElement element) {
		// TODO: ?
		return new ScalarNode();
	}

	private static SchemaNode ReadMultiReferenceConverter(in JsonElement element) {
		// TODO: Reference node
		// element["targets"] = array of target sheet names
		return new ScalarNode();
	}

	private static SchemaNode ReadSheetLinkConverter(in JsonElement element) {
		// TODO: Reference node
		// element["target"] = target sheet name
		return new ScalarNode();
	}

	private static SchemaNode ReadTomestoneOrItemReferenceConverter(in JsonElement element) {
		// TODO: ?
		return new ScalarNode();
	}

	private static SchemaNode ReadComplexLinkConverter(in JsonElement element) {
		// TODO: Reference node.
		return new ScalarNode();
	}
}
