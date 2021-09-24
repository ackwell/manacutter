namespace Manacutter.Services.Definitions.SaintCoinach;

#pragma warning disable CS8618

public class SheetDefinition {
	public List<ColumnDefinition> Definitions { get; set; }
}

// TODO: rename?
public class ColumnDefinition {
	public int Index { get; set; } = 0;
	public string? Name { get; set; }
}
