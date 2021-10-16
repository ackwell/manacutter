using Manacutter.Common.Schema;
using Manacutter.Readers;

namespace Manacutter.Services.REST;

// TODO: Not convinced by this name at all
public class RESTBuilder : SchemaWalker<SchemaWalkerContext, object> {
	// TODO: if we put this in DI, this'll need to be context i guess? or built on demand? probably the latter?
	private readonly IRowReader rowReader;

	public RESTBuilder(
		IRowReader rowReader
	) {
		this.rowReader = rowReader;
	}

	public object Visit(SchemaNode node) {
		return this.Visit(node, new SchemaWalkerContext());
	}

	public override object VisitSheets(SheetsNode node, SchemaWalkerContext context) {
		throw new NotImplementedException();
	}

	public override object VisitStruct(StructNode node, SchemaWalkerContext context) {
		return this.WalkStruct(node, context);
	}

	public override object VisitArray(ArrayNode node, SchemaWalkerContext context) {
		var elementWidth = node.Type.Size;

		var value = new List<object>();
		for (uint index = 0; index < node.Count; index++) {
			value.Add(this.WalkArray(node, context with {
				Offset = context.Offset + (index * elementWidth)
			}));
		}

		return value;
	}

	public override object VisitScalar(ScalarNode node, SchemaWalkerContext context) {
		return this.rowReader.Read(node, context.Offset);
	}
}
