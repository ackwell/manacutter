using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Types.Relay;
using Manacutter.Readers;

namespace Manacutter.Services.GraphQL.GraphQLDotNet;

// TODO: Build a proper relay structure
public class PluralSheetFieldType : FieldType {
	public PluralSheetFieldType(FieldType baseField, ISheetReader sheet) : base() {
		// TODO: actually pluralise this
		this.Name = $"{baseField.Name}_plural";

		this.ResolvedType = new SheetConnectionGraphType(baseField.Name, baseField.ResolvedType);
		// TODO: how do we handle subrowids?
		this.Arguments = new QueryArguments(
			new QueryArgument<NonNullGraphType<IntGraphType>>() { Name = "first" },
			new QueryArgument<StringGraphType>() { Name = "after" },
			new QueryArgument<NonNullGraphType<IntGraphType>>() { Name = "last" },
			new QueryArgument<StringGraphType>() { Name = "before" }
		);
		// TODO: the => { } is just to force resolution in the manner I want currently. This will likely need to become a "connection execution context" of some kind to pass info down to the connection type for pagination.
		this.Resolver = new FuncFieldResolver<object>(context => new { /* TODO */ });

		return;

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

// TODO: I mean realistically this could be a method on the plural field?
class SheetConnectionGraphType : ObjectGraphType {
	public SheetConnectionGraphType(string name, IGraphType sheetType) {
		this.Name = $"{name}Connection";

		this.Field("edges", new ListGraphType(new SheetEdgeGraphType(name, sheetType)), resolve: context => {
			// TODO
			return new[] {1};
		});

		this.Field<NonNullGraphType<PageInfoType>>("pageInfo", resolve: context => new { /* TODO */ });
	}
}

class SheetEdgeGraphType : ObjectGraphType {
	public SheetEdgeGraphType(string name, IGraphType sheetType) {
		this.Name = $"{name}Edge";

		this.Field("node", sheetType, resolve: context => new { /* TODO */ });

		// TODO: should this be an ID or are we divorcing cursors from IDs entirely?
		this.Field<StringGraphType>("cursor", resolve: context => "TODO");
	}
}
