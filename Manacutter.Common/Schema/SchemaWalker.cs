namespace Manacutter.Common.Schema;

/// <summary>Base visiting context maintained by the schema walker.</summary>
public record SchemaWalkerContext {
	/// <summary>Current column offset.</summary>
	public uint Offset { get; init; }
}

public abstract class SchemaWalker<TContext, TReturn>
	: ISchemaVisitor<TContext, TReturn>
	where TContext : SchemaWalkerContext {

	public TReturn Visit(SchemaNode node, TContext context) {
		return node.Accept(this, context);
	}

	public abstract TReturn VisitSheets(SheetsNode node, TContext context);

	/// <summary>Walk the provided sheets node, visiting all sheet schemas.</summary>
	/// <param name="node">The sheets node to walk.</param>
	/// <param name="context">Visiting context.</param>
	/// <param name="contextTransform">Transformation to apply to the visiting context on a per-sheet basis.</param>
	/// <returns>Visitor results mapped to their sheet names.</returns>
	protected IReadOnlyDictionary<string, TReturn> WalkSheets(
		SheetsNode node,
		TContext context,
		Func<TContext, string, SchemaNode, TContext>? contextTransform = null
	) {
		return node.Sheets.ToDictionary(
			pair => pair.Key,
			pair => {
				var newContext = context;
				if (contextTransform is not null) {
					newContext = contextTransform(newContext, pair.Key, pair.Value);
				}
				return this.Visit(pair.Value, newContext);
			}
		);
	}

	public abstract TReturn VisitStruct(StructNode node, TContext context);

	// TODO: I'm not sure how happy I am about this optional param. Investigate alternatives. It may be worth using an entry/exit pattern, as that may also permit using mutable state, which is cheaper.
	/// <summary>Walk the provided struct node, visiting all children.</summary>
	/// <param name="node">The struct node to walk.</param>
	/// <param name="context">Visiting context.</param>
	/// <param name="contextTransform">Transformation to apply to the visiting context on a per-field basis.</param>
	/// <returns>Visitor results mapped to their field names.</returns>
	protected IReadOnlyDictionary<string, TReturn> WalkStruct(
		StructNode node,
		TContext context,
		Func<TContext, string, SchemaNode, TContext>? contextTransform = null
	) {
		return node.Fields.ToDictionary(
			pair => pair.Key,
			pair => {
				var (offset, node) = pair.Value;

				var newContext = context with {
					Offset = context.Offset + offset
				};
				if (contextTransform is not null) {
					newContext = contextTransform(newContext, pair.Key, node);
				}
				return this.Visit(node, newContext);
			}
		);
	}

	public abstract TReturn VisitArray(ArrayNode node, TContext context);

	/// <summary>Walk the provided array node, visiting the child node.</summary>
	/// <param name="node">The array node to walk.</param>
	/// <param name="context">Visiting context.</param>
	/// <returns>Visitor result.</returns>
	protected TReturn WalkArray(ArrayNode node, TContext context) {
		return this.Visit(node.Type, context with {
			Offset = context.Offset
		});
	}

	public abstract TReturn VisitScalar(ScalarNode node, TContext context);
}
