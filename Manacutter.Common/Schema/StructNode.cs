namespace Manacutter.Common.Schema;

/// <summary> Representation of a group of named nodes.</summary>
public record StructNode : SchemaNode {
	/// <summary>Mapping of names to their associated node trees.</summary>
	public IReadOnlyDictionary<string, (uint Offset, SchemaNode Node)> Fields { get; init; }

	public StructNode(
		IReadOnlyDictionary<string, (uint, SchemaNode)> fields
	) {
		this.Fields = fields;
	}

	public override uint Size {
		get => this.Fields.Values.Aggregate(
			(uint)0,
			(size, value) => size + value.Node.Size
		);
	}

	public override TReturn Accept<TContext, TReturn>(
		ISchemaVisitor<TContext, TReturn> visitor,
		TContext context
	) {
		return visitor.VisitStruct(this, context);
	}
}
