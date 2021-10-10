using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Types.Relay;
using Manacutter.Readers;
using System.Text;

namespace Manacutter.Services.GraphQL.GraphQLDotNet;

record ConnectionContext {
	public IList<ExecutionContext> ExecutionContexts { get; init; }
	public bool HasNextPage { get; init; }

	public ConnectionContext(IList<ExecutionContext> executionContexts) {
		this.ExecutionContexts = executionContexts;
	}
}

public class PluralSheetFieldType : FieldType {
	public PluralSheetFieldType(FieldType baseField, ISheetReader sheet) : base() {
		if (baseField.ResolvedType is null) {
			throw new ArgumentNullException(nameof(baseField.ResolvedType));
		}

		// TODO: actually pluralise this
		this.Name = $"{baseField.Name}_plural";

		this.ResolvedType = new SheetConnectionGraphType(baseField.Name, baseField.ResolvedType);
		this.Arguments = new QueryArguments(
			new QueryArgument<NonNullGraphType<IntGraphType>>() { Name = "first" },
			new QueryArgument<StringGraphType>() { Name = "after" }
		);

		this.Resolver = new FuncFieldResolver<object>(context => {
			// Read and validate arguments.
			var first = context.GetArgument<int>("first");
			var after = context.GetArgument<string?>("after");

			// I'm using int for first due to relay schema, but the schema _also_ says it should throw on <0, soo...?
			if (first < 0) {
				throw new ExecutionError("Invalid `first` value.");
			}

			// TODO: value should be configurable
			first = Math.Min(first, 100);

			// Translate the specified cursor into IDs, if any is available.
			var (startRowId, startSubRowId) = after is not null
				? CursorUtils.CursorToIDs(after)
				: ((uint?)null, (uint?)null);

			// Set up a delegate to resolve execution contexts, sending them via the base field's resolver if there is any.
			var baseExecContext = (ExecutionContext)context.Source!;
			Func<ExecutionContext, ExecutionContext> resolveSource = baseField.Resolver is null
				? (ExecutionContext source) => source
				: (ExecutionContext source) => {
					var newContext = new ResolveFieldContext<ExecutionContext>(context) { Source = source };
					return (ExecutionContext)baseField.Resolver.Resolve(newContext)!;
				};

			var execContexts = context.ArrayPool.Rent<ExecutionContext>(first);

			// Iterate through sheet's rows until we reach the requested count, or exhaust the sheet.
			// We're starting at -1 if there's a start point, as we need to fetch count _after_ the cursror.
			int count = startRowId is null ? 0 : -1;
			var rowEnumerator = sheet.EnumerateRows(startRowId, startSubRowId).GetEnumerator();
			for (; count < first; count++) {
				if (!rowEnumerator.MoveNext()) { break; }
				if (count < 0) { continue; }
				execContexts[count] = resolveSource(baseExecContext with { Row = rowEnumerator.Current });
			}

			return new ConnectionContext(execContexts.Constrained(count)) {
				// We have a next page if the enumerator still has items after the collected list.
				HasNextPage = rowEnumerator.MoveNext(),
			};
		});
	}
}

class SheetConnectionGraphType : ObjectGraphType {
	public SheetConnectionGraphType(string name, IGraphType sheetType) {
		this.Name = $"{name}Connection";

		this.Field("edges", new ListGraphType(new SheetEdgeGraphType(name, sheetType)), resolve: context => {
			return ((ConnectionContext)context.Source!).ExecutionContexts;
		});

		this.Field<NonNullGraphType<PageInfoType>>("pageInfo", resolve: context => {
			var connectionContext = (ConnectionContext)context.Source!;
			var execContexts = connectionContext.ExecutionContexts;

			// TODO: this should probably be lazy? idk.
			return new {
				HasPreviousPage = false,
				HasNextPage = connectionContext.HasNextPage,
				StartCursor = CursorUtils.RowToCursor(execContexts.FirstOrDefault()?.Row),
				EndCursor = CursorUtils.RowToCursor(execContexts.LastOrDefault()?.Row),
			};
		});
	}
}

class SheetEdgeGraphType : ObjectGraphType {
	public SheetEdgeGraphType(string name, IGraphType sheetType) {
		this.Name = $"{name}Edge";

		this.Field("node", sheetType, resolve: context => context.Source);

		this.Field<StringGraphType>("cursor", resolve: context => {
			var execContext = (ExecutionContext)context.Source!;
			return CursorUtils.RowToCursor(execContext.Row);
		});
	}
}

static class CursorUtils {
	static readonly char separator = '/';

	public static string? RowToCursor(IRowReader? row) {
		if (row is null) { return null; }
		var str = $"{row.RowID}{separator}{row.SubRowID}";
		return Convert.ToBase64String(Encoding.UTF8.GetBytes(str));
	}

	public static (uint, uint) CursorToIDs(string cursor) {
		var str = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
		var ids = str.Split(separator, 2);
		return (
			uint.Parse(ids[0]),
			uint.Parse(ids[1])
		);
	}
}
