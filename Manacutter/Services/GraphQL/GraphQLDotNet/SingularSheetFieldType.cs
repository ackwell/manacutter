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

			// TODO: this is mutating. That's bad.
			var execContext = (ExecutionContext)context.Source!;
			execContext.Sheet = sheet;
			execContext.Row = sheet.GetRow(rowId);

			return baseField.Resolver is null
				? context
				: baseField.Resolver.Resolve(context);
		});
	}
}
