using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using Manacutter.Readers;

namespace Manacutter.Services.GraphQL.GraphQLDotNet;

// TODO: Build a proper relay structure
public class PluralSheetFieldType : FieldType {
	public PluralSheetFieldType(FieldType baseField, ISheetReader sheet) : base() {
		// TODO: actually pluralise this
		this.Name = $"{baseField.Name}_plural";
		this.ResolvedType = new ListGraphType(baseField.ResolvedType);
		this.Resolver = new FuncFieldResolver<object>(context => {
			// TODO: Temp testing stuff
			var execContexts = context.ArrayPool.Rent<ExecutionContext>(2);

			for (uint index = 0; index <= 1; index++) {
				var newContext = new ResolveFieldContext<ExecutionContext>(context);
				var newSource = newContext.Source! with {
					Row = sheet.GetRow(index + 10)
				};
				newContext.Source = newSource;
				execContexts[index] = baseField.Resolver is null
					? newSource
					: (ExecutionContext)baseField.Resolver.Resolve(newContext)!;
			}

			return execContexts.Constrained(2);
		});
	}
}
