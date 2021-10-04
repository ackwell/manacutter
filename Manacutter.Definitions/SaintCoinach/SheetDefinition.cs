namespace Manacutter.Definitions.SaintCoinach;

#pragma warning disable CS8618

// TODO: StC has classes for all this shit, but it desers a JObject and builds manually. Possibly the "best" long-term solution, look into viability.

internal class SheetDefinition {
	public List<DefinitionEntry> Definitions { get; set; }
}

internal class DefinitionEntry {
	public uint Index { get; set; } = 0;
	public string? Name { get; set; }
	public string? Type { get; set; }
	public uint? Count { get; set; }
	public DefinitionEntry? Definition { get; set; }
	public List<DefinitionEntry>? Members { get; set; }
	public Converter? Converter { get; set; }
}

internal class Converter {
	public string Type { get; set; }
	public string? Target { get; set; }
	// TODO: Fields like "links" for complexlink, etc
}
