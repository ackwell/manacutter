using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using Manacutter.Readers;

namespace Manacutter.Services.GraphQL.GraphQLDotNet;

public class SingularSheetFieldType : FieldType {
	public SingularSheetFieldType(FieldType baseField, ISheetReader sheet) : base() {
		this.Name = baseField.Name;
		this.ResolvedType = baseField.ResolvedType;

		this.Arguments = new QueryArguments(
			new QueryArgument<NonNullGraphType<UIntGraphType>>() { Name = "rowId" }
		);
		if (sheet.HasSubrows) {
			this.Arguments.Add(
				new QueryArgument<NonNullGraphType<UIntGraphType>>() { Name = "subRowId" }
			);
		}

		this.Resolver = new FuncFieldResolver<object>(context => {
			var rowId = context.GetArgument<uint>("rowId");

			uint? subRowId = sheet.HasSubrows
				? context.GetArgument<uint>("subRowId")
				: null;

			var row = sheet.GetRow(rowId, subRowId);

			if (row is null) {
				return null;
			}

			var newContext = new ResolveFieldContext<ExecutionContext>(context);
			newContext.Source = newContext.Source! with {
				Row = row,
			};

			return baseField.Resolver is null
				? newContext.Source
				: baseField.Resolver.Resolve(newContext);
		});
	}
}
