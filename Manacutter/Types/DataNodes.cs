namespace Manacutter.Types;

// TODO: this should be elsewhere
public interface INodeVisitor<TContext, TReturn> {
	public TReturn VisitStruct(StructNode node, TContext context);
	public TReturn VisitArray(ArrayNode node, TContext context);
	public TReturn VisitScalar(ScalarNode node, TContext context);
}

public record NodeWalkerContext {
	public uint Offset { get; init; }
}

public abstract class NodeWalker<TContext, TReturn> : INodeVisitor<TContext, TReturn> where TContext : NodeWalkerContext {
	public TReturn Visit(DataNode node, TContext context) {
		return node.Accept(this, context);
	}
	public abstract TReturn VisitStruct(StructNode node, TContext context);
	protected IDictionary<string, TReturn> WalkStruct(StructNode node, TContext context) {
		return node.Fields.ToDictionary(
			pair => pair.Key,
			pair => this.Visit(pair.Value, context with {
				Offset = context.Offset + pair.Value.Offset
			})
		);
	}
	public abstract TReturn VisitArray(ArrayNode node, TContext context);
	protected TReturn WalkArray(ArrayNode node, TContext context) {
		return this.Visit(node.Type, context with {
			Offset = context.Offset + node.Type.Offset
		});
	}
	public abstract TReturn VisitScalar(ScalarNode node, TContext context);
}

public abstract class DataNode {
	/// <summary>Column offset within this node's parent.</summary>
	public uint Offset { get; init; } = 0;

	/// <summary>Size of this node, in columns.</summary>
	public abstract uint Size { get; }

	public abstract TReturn Accept<TContext, TReturn>(INodeVisitor<TContext, TReturn> visitor, TContext context);
}

public class StructNode : DataNode {
	public IDictionary<string, DataNode> Fields { get; }

	public override uint Size {
		get => this.Fields.Values.Aggregate(
			(uint)0,
			(size, node) => size + node.Size
		);
	}

	public StructNode(
		IDictionary<string, DataNode> fields
	) {
		this.Fields = fields;
	}

	public override TReturn Accept<TContext, TReturn>(INodeVisitor<TContext, TReturn> visitor, TContext context) {
		return visitor.VisitStruct(this, context);
	}
}

public class ArrayNode : DataNode {
	public DataNode Type { get; }
	public uint Count { get; }

	public override uint Size {
		get => this.Type.Size * this.Count;
	}

	public ArrayNode(
		DataNode type,
		uint length
	) {
		this.Type = type;
		this.Count = length;
	}

	public override TReturn Accept<TContext, TReturn>(INodeVisitor<TContext, TReturn> visitor, TContext context) {
		return visitor.VisitArray(this, context);
	}
}

public class ScalarNode : DataNode {
	public ScalarType Type { get; init; } = ScalarType.Unknown;

	public override uint Size { get => 1; }

	public override TReturn Accept<TContext, TReturn>(INodeVisitor<TContext, TReturn> visitor, TContext context) {
		return visitor.VisitScalar(this, context);
	}
}

public enum ScalarType {
	Unknown = 0,
	String,
	Boolean,
	Int8,
	UInt8,
	Int16,
	UInt16,
	Int32,
	UInt32,
	Int64,
	UInt64,
	Float,
}
