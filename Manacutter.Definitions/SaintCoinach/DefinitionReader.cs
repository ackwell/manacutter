using Manacutter.Common.Schema;
using System.Text.Json;

namespace Manacutter.Definitions.SaintCoinach;

internal static class DefinitionReader {
	/// <seealso href="https://github.com/xivapi/SaintCoinach/blob/800eab3e9dd4a2abc625f53ce84dad24c8579920/SaintCoinach/Ex/Relational/Definition/SheetDefinition.cs#L157">SheetDefinition.cs#L157</seealso>
	/// <seealso href="https://github.com/xivapi/SaintCoinach/blob/800eab3e9dd4a2abc625f53ce84dad24c8579920/SaintCoinach/Ex/Relational/Definition/PositionedDataDefinition.cs#L71">PositionedDataDefinition.cs#L71</seealso>
	internal static SchemaNode ReadSheetDefinition(in JsonElement element) {
		var fields = new Dictionary<string, (uint, SchemaNode)>();

		var definitions = element.GetProperty("definitions");
		foreach (var definition in definitions.EnumerateArray()) {
			// PositionedDataDefinition inlined as it's only used in one location, and makes setting up the struct fields simpler
			var index = definition.TryGetProperty("index", out var property)
				? property.GetUInt32()
				: 0;

			var (node, name) = ReadDataDefinition(definition);

			fields.Add(
				name ?? $"Unnamed{index}",
				(index, node)
			);
		}

		return new StructNode(fields);
	}

	/// <seealso href="https://github.com/xivapi/SaintCoinach/blob/800eab3e9dd4a2abc625f53ce84dad24c8579920/SaintCoinach/Ex/Relational/Definition/IDataDefinition.cs#L34">IDataDefinition.cs#L34</seealso>
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

	/// <seealso href="https://github.com/xivapi/SaintCoinach/blob/800eab3e9dd4a2abc625f53ce84dad24c8579920/SaintCoinach/Ex/Relational/Definition/SingleDataDefinition.cs#L66">SingleDataDefinition.cs#L66</seealso>
	private static (SchemaNode, string?) ReadSingleDataDefinition(in JsonElement element) {
		var name = element.GetProperty("name").GetString();
		var converterExists = element.TryGetProperty("converter", out var converter);

		if (!converterExists) {
			return (new ScalarNode(), name);
		}

		var type = converter.TryGetProperty("type", out var property)
			? property.GetString()
			: null;

		// TODO: There's also a "quad" type with a converter but I've got no idea how it's instantiated.
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

	/// <seealso href="https://github.com/xivapi/SaintCoinach/blob/800eab3e9dd4a2abc625f53ce84dad24c8579920/SaintCoinach/Ex/Relational/Definition/GroupDataDefinition.cs#L125">GroupDataDefinition.cs#L125</seealso>
	private static (SchemaNode, string?) ReadGroupDataDefinition(in JsonElement element) {
		var fields = new Dictionary<string, (uint, SchemaNode)>();

		uint size = 0;
		var members = element.GetProperty("members");
		foreach (var member in members.EnumerateArray()) {
			var (childNode, childName) = ReadDataDefinition(member);
			fields.Add(
				childName ?? $"Unnamed{size++}",
				(size, childNode)
			);
			size += childNode.Size;
		}

		var node = new StructNode(fields);
		var lcs = fields.Keys.Aggregate(Helpers.LongestCommonSubsequence);
		var name = lcs != "" ? lcs : null;

		return (new StructNode(fields), name);
	}

	/// <seealso href="https://github.com/xivapi/SaintCoinach/blob/800eab3e9dd4a2abc625f53ce84dad24c8579920/SaintCoinach/Ex/Relational/Definition/RepeatDataDefinition.cs#L85">RepeatDataDefinition.cs#L85</seealso>
	private static (SchemaNode, string?) ReadRepeatDataDefinition(in JsonElement element) {
		var (childNode, childName) = ReadDataDefinition(element.GetProperty("definition"));
		return (
			new ArrayNode(childNode, element.GetProperty("count").GetUInt32()),
			childName
		);
	}

	/// <seealso href="https://github.com/xivapi/SaintCoinach/blob/800eab3e9dd4a2abc625f53ce84dad24c8579920/SaintCoinach/Ex/Relational/ValueConverters/ColorConverter.cs#L46">ColorConverter.cs#L46</seealso>
	private static SchemaNode ReadColorConverter(in JsonElement element) {
		// TODO: ?
		return new ScalarNode();
	}

	/// <seealso href="https://github.com/xivapi/SaintCoinach/blob/800eab3e9dd4a2abc625f53ce84dad24c8579920/SaintCoinach/Ex/Relational/ValueConverters/GenericReferenceConverter.cs#L33">GenericReferenceConverter.cs#L33</seealso>
	private static SchemaNode ReadGenericReferenceConverter(in JsonElement element) {
		// TODO: ?
		return new ScalarNode();
	}

	/// <seealso href="https://github.com/xivapi/SaintCoinach/blob/800eab3e9dd4a2abc625f53ce84dad24c8579920/SaintCoinach/Ex/Relational/ValueConverters/IconConverter.cs#L33">IconConverter.cs#L33</seealso>
	private static SchemaNode ReadIconConverter(in JsonElement element) {
		// TODO: ?
		return new ScalarNode();
	}

	/// <seealso href="https://github.com/xivapi/SaintCoinach/blob/800eab3e9dd4a2abc625f53ce84dad24c8579920/SaintCoinach/Ex/Relational/ValueConverters/MultiReferenceConverter.cs#L50">MultiReferenceConverter.cs#L50</seealso>
	private static SchemaNode ReadMultiReferenceConverter(in JsonElement element) {
		var targets = element.GetProperty("targets").EnumerateArray()
			.Select(target => target.GetString())
			.Where(target => target is not null)
			.Select(target => new ReferenceTarget(target!))
			.ToList();

		return new ReferenceNode() {
			Targets = targets,
		};
	}

	/// <seealso href="https://github.com/xivapi/SaintCoinach/blob/800eab3e9dd4a2abc625f53ce84dad24c8579920/SaintCoinach/Ex/Relational/ValueConverters/SheetLinkConverter.cs#L40">SheetLinkConverter.cs#L40</seealso>
	private static SchemaNode ReadSheetLinkConverter(in JsonElement element) {
		var target = element.GetProperty("target").GetString();

		if (target is null) {
			throw new ArgumentNullException("target");
		}

		return new ReferenceNode() {
			Targets = new[] {
				new ReferenceTarget(target),
			},
		};
	}

	/// <seealso href="https://github.com/xivapi/SaintCoinach/blob/800eab3e9dd4a2abc625f53ce84dad24c8579920/SaintCoinach/Ex/Relational/ValueConverters/TomestoneOrItemReferenceConverter.cs#L54">TomestoneOrItemReferenceConverter.cs#L54</seealso>
	private static SchemaNode ReadTomestoneOrItemReferenceConverter(in JsonElement element) {
		// TODO: ?
		return new ScalarNode();
	}

	/// <seealso href="https://github.com/xivapi/SaintCoinach/blob/800eab3e9dd4a2abc625f53ce84dad24c8579920/SaintCoinach/Ex/Relational/ValueConverters/ComplexLinkConverter.cs#L143">ComplexLinkConverter.cs#L143</seealso>
	private static SchemaNode ReadComplexLinkConverter(in JsonElement element) {
		/*
		 * jesus fucking christ okay let's follow this hell hole of fucking logic thanks stc you piece of shit
		 * - read key as int32
		 * - iterate over links (link = iter element["links"])
		 *   - check when clause (link["when"]), if it doesn't match continue to next link
		 *   - fetch row (link["sheet"] or link["sheets"])
		 *     - can basically consider single sheet to be same as a one item array, it's not doing much special there
		 *     - run found sheet through the "row producer" (link["key"])
		 *		   - if no key specified, fetch row by ID
		 *		   - if key specified, fetch row by searching for row with column=keyname with matching value
		 *		   - if none of the above exist, continue to next link
		 *   - project the result (link["project"])
		 *     - if no projection specified, return row as-is
		 *     - if projection specified, return value of field in specified row
		 *     
		 * so what do we actually need from that?
		 * - we need the when conditional, as that plays into the main link loop
		 * - i think we need the key, too - in the weeklybingorewarddata->tomestonesitem link, it needs to link on non-key row for the relation to work
		 *   - i've only seen two of these, though, it's certainly not common. Possibly just... leave it?
		 * - projection seems pretty UI-focused, and is primarily being used to actually skip over a potentially meaningful table. i'd err to ignoring it.
		 */

		// Build a list of targets from the complexlink's links
		var targets = new List<ReferenceTarget>();
		foreach (var link in element.GetProperty("links").EnumerateArray()) {
			// Check if there's a when clause, transforming it into a condition if any is found.
			var condition = link.TryGetProperty("when", out var when)
				? ReadWhenClause(when)
				: null;

			// Get the target sheet column, if any.
			var key = link.TryGetProperty("key", out var keyProperty)
				? keyProperty.GetString()
				: null;

			// If there's a key for a singular sheet, add it.
			var sheet = link.TryGetProperty("sheet", out var sheetProperty)
				? sheetProperty.GetString()
				: null;
			if (sheet is not null) {
				targets.Add(new ReferenceTarget(sheet) {
					Condition = condition,
					Field = key,
				});
			}

			// Check if there's a sheets key, and skip out if not.
			var hasSheets = link.TryGetProperty("sheets", out var sheetsProperty);
			if (!hasSheets) {
				continue;
			}

			// Enumerate over the declared sheets, mapping to a reference target for each.
			var newTargets = sheetsProperty.EnumerateArray()
				.Select(sheet => sheet.GetString())
				.Where(sheet => sheet is not null) 
				.Select(sheet => new ReferenceTarget(sheet!) {
					Condition = condition,
					Field = key,
				});

			targets.AddRange(newTargets);
		}

		return new ReferenceNode() {
			Targets = targets,
		};
	}

	private static ReferenceCondition ReadWhenClause(in JsonElement element) {
		var key = element.GetProperty("key").GetString();
		if (key is null) {
			throw new ArgumentNullException("key");
		}

		// Coinach schema only seems to reference uints here. Keep an eye on this.
		var value = element.GetProperty("value").GetUInt32();

		return new ReferenceCondition(key, value);
	}
}
