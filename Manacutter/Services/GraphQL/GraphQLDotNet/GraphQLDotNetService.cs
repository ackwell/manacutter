using GraphQL;
using GraphQL.Resolvers;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using Manacutter.Services.Definitions;
using Manacutter.Services.Readers;
using System.Collections.Immutable;
using System.Text.Json;

namespace Manacutter.Services.GraphQL.GraphQLDotNet;

public class GraphQLDotNetService : IGraphQLService {
	private readonly IReader reader;

	public GraphQLDotNetService(
		IReader reader
	) {
		this.reader = reader;
	}

	// TODO: Cache schemas or something
	public IGraphQLSchema GetSchema(IDefinitionProvider definitionProvider) {
		// TODO: Get this from... something. It's a tossup between reader (as it's the source of truth for game data), and definitions (as it's the source of truth for what we can read). Leaning towards the latter currently, which will require some interface additions.
		// TODO: sheet name needs standardisation across the board on stuff like caps.
		var sheetNames = new[] { "Action", "Item" };
		var builder = new FieldBuilder();

		var graphType = new ObjectGraphType() { Name = "Query" };

		foreach (var sheetName in sheetNames) {
			var sheet = this.reader.GetSheet(sheetName);
			if (sheet is null) { continue; }
			var sheetNode = definitionProvider.GetRootNode(sheetName);

			// Build & name the core field type for the sheet
			var fieldType = builder.Visit(sheetNode, new FieldBuilderContext(sheet) {
				Path = ImmutableList.Create(sheetName)
			});
			if (fieldType.ResolvedType is not null) {
				fieldType.ResolvedType.Name = sheetName;
				this.AddIDFields(fieldType.ResolvedType);
			}

			// Add query fields to the root schema type
			graphType.AddField(this.BuildSheetSingular(fieldType, sheet));
			graphType.AddField(this.BuildSheetPlural(fieldType, sheet));
		}

		return new GraphQLDotNetSchema(graphType);
	}

	private void AddIDFields(IGraphType graphType) {
		if (graphType is not ObjectGraphType) {
			return;
		}

		var objectGraphType = (ObjectGraphType)graphType;
		objectGraphType.Field("rowId", new UIntGraphType(), resolve: context => {
			var execContext = (ExecutionContext)context.Source!;
			return execContext.Row?.RowID;
		});
		// TODO: subrow
	}

	private FieldType BuildSheetSingular(FieldType fieldType, ISheetReader sheet) {
		return new FieldType() {
			Name = fieldType.Name,
			ResolvedType = fieldType.ResolvedType,
			Arguments = new QueryArguments(
				new QueryArgument<NonNullGraphType<UIntGraphType>>() { Name = "rowId" }
			),
			Resolver = new FuncFieldResolver<object>(context => {
				var rowId = context.GetArgument<uint>("rowId");
				// TODO: subrow

				var execContext = (ExecutionContext)context.Source!;
				execContext.Sheet = sheet;
				execContext.Row = sheet.GetRow(rowId);

				return fieldType.Resolver is null
					? context
					: fieldType.Resolver.Resolve(context);
			})
		};
	}

	private FieldType BuildSheetPlural(FieldType fieldType, ISheetReader sheet) {
		// TODO: Build a proper relay structure
		return new FieldType() {
			// TODO: actually pluralise this
			Name = $"{fieldType.Name}_plural",
			ResolvedType = new ListGraphType(fieldType.ResolvedType),
			Resolver = new FuncFieldResolver<object>(context => {
				// TODO: Temp testing stuff
				var execContexts = context.ArrayPool.Rent<ExecutionContext>(2);
				execContexts[0] = new ExecutionContext() { Sheet = sheet, Row = sheet.GetRow(1) };
				execContexts[1] = new ExecutionContext() { Sheet = sheet, Row = sheet.GetRow(2) };
				return execContexts.Constrained(2);
			})
		};
	}
}

public class ExecutionContext {
	public ISheetReader? Sheet { get; set; }
	public IRowReader? Row { get; set; }
	// TODO: This is only really _nessecary_ for array nodes - is there a better way to handle it?
	public uint Offset { get; set; } = 0;
}

public class GraphQLDotNetSchema : IGraphQLSchema {
	private readonly Schema schema;

	public GraphQLDotNetSchema(
		ObjectGraphType rootGraphType
	) {
		this.schema = new Schema() {
			Query = rootGraphType
		};
	}

	// TODO: think about the variables type a bit.
	public Task<string> Query(string query, JsonElement variables) {
		return this.schema.ExecuteAsync(options => {
			options.Query = query;
			options.Inputs = variables.ToInputs();
			options.Root = new ExecutionContext();
		});
	}
}
