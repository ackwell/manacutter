using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using Manacutter.Services.Readers;

namespace Manacutter.Services.GraphQL.GraphQLDotNet;

// TODO: Build a proper relay structure
public class PluralSheetFieldType : FieldType {
	public PluralSheetFieldType(FieldType baseField, ISheetReader sheet) : base() {
		// TODO: actually pluralise this
		this.Name = $"{baseField.Name}_plural";
		this.ResolvedType = new ListGraphType(baseField.ResolvedType);
		this.Resolver = new FuncFieldResolver<object>(context => {
			// TODO: This is currently not calling down to the base field resolver. It should be. Fix.

			// TODO: Temp testing stuff
			var execContexts = context.ArrayPool.Rent<ExecutionContext>(2);
			execContexts[0] = new ExecutionContext() { Sheet = sheet, Row = sheet.GetRow(1) };
			execContexts[1] = new ExecutionContext() { Sheet = sheet, Row = sheet.GetRow(2) };
			return execContexts.Constrained(2);
		});
	}
}
