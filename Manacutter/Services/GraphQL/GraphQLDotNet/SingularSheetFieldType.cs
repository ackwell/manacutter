using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using Manacutter.Services.Readers;

namespace Manacutter.Services.GraphQL.GraphQLDotNet;

public class SingularSheetFieldType : FieldType {
	public SingularSheetFieldType(FieldType baseField, ISheetReader sheet) : base() {
		this.Name = baseField.Name;
		this.ResolvedType = baseField.ResolvedType;
		this.Arguments = new QueryArguments(
			new QueryArgument<NonNullGraphType<UIntGraphType>>() { Name = "rowId" }
			// TODO: subrow on sheets that support them
		);
		this.Resolver = new FuncFieldResolver<object>(context => {
			var rowId = context.GetArgument<uint>("rowId");

			var newContext = new ResolveFieldContext<ExecutionContext>(context);
			newContext.Source = newContext.Source! with {
				Sheet = sheet,
				Row = sheet.GetRow(rowId),
			};

			return baseField.Resolver is null
				? newContext
				: baseField.Resolver.Resolve(newContext);
		});
	}
}
