using Manacutter.Common.Schema;
using Manacutter.Readers;

namespace Manacutter.Services.REST;

// TODO: Work out a format for a "column filter" of sorts that can be a context/walkable system (list of things? idk). Doing that, we can remove a reasonable amount of the logic in the current sheets controller, and make this logic + walking considerably more self-contained.

public record RESTBuilderContext : SchemaWalkerContext {
	public IRowReader RowReader { get; init; }

	public RESTBuilderContext(IRowReader rowReader) : base() {
		this.RowReader = rowReader;
	}
}

// TODO: Not convinced by this name at all
// TODO: Consider an interface if this becomes non-trivial i guess
public class RESTBuilder : SchemaWalker<RESTBuilderContext, object> {
	public object Read(SchemaNode node, IRowReader rowReader) {
		return this.Visit(node, new RESTBuilderContext(rowReader));
	}

	public override object VisitSheets(SheetsNode node, RESTBuilderContext context) {
		throw new NotImplementedException();
	}

	public override object VisitStruct(StructNode node, RESTBuilderContext context) {
		return this.WalkStruct(node, context);
	}

	public override object VisitArray(ArrayNode node, RESTBuilderContext context) {
		var elementWidth = node.Type.Size;

		var value = new List<object>();
		for (uint index = 0; index < node.Count; index++) {
			value.Add(this.WalkArray(node, context with {
				Offset = context.Offset + (index * elementWidth)
			}));
		}

		return value;
	}

	public override object VisitScalar(ScalarNode node, RESTBuilderContext context) {
		return context.RowReader.ReadColumn(context.Offset);
	}
}
