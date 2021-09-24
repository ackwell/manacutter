using GraphQL;
using GraphQL.Resolvers;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using Manacutter.Services.Definitions;
using Manacutter.Services.Readers;
using Manacutter.Types;
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
		var sheetNames = new[] { "Action" };

		var graphType = new ObjectGraphType() { Name = "Query" };

		foreach (var sheetName in sheetNames) {
			var sheet = this.reader.GetSheet(sheetName);
			if (sheet is null) { continue; }
			var sheetNode = definitionProvider.GetRootNode(sheetName);

			var fieldType = this.BuildFieldType(sheetNode, sheet);
			fieldType.Name = sheetName;
			if (fieldType.ResolvedType is not null) {
				fieldType.ResolvedType.Name = sheetName;
			}

			graphType.AddField(this.BuildSheetSingular(fieldType, sheet));
		}

		return new GraphQLDotNetSchema(graphType);
	}

	private FieldType BuildSheetSingular(FieldType fieldType, ISheetReader sheet) {
		return new FieldType() {
			Name = fieldType.Name,
			ResolvedType = fieldType.ResolvedType,
			Arguments = new QueryArguments(
				new QueryArgument<NonNullGraphType<UIntGraphType>>() { Name = "id" }
			),
			Resolver = new FuncFieldResolver<object>(context => {
				// TODO: we really need to call down to the base field type, data should be moving around in a class we control
				var id = context.GetArgument<uint>("id");
				return sheet.GetRow(id);
			})
		};
	}

	private FieldType BuildFieldType(DataNode node, ISheetReader sheet) {
		return node switch {
			StructNode structNode => this.BuildStructFieldType(structNode, sheet),
			ScalarNode scalarNode => this.BuildScalarFieldType(scalarNode, sheet),
			_ => throw new NotImplementedException(),
		};
	}

	private FieldType BuildStructFieldType(StructNode node, ISheetReader sheet) {
		var type = new ObjectGraphType();

		foreach (var field in node.Fields) {
			// TODO: lmao
			var name = field.Key
				.Replace('{', '_').Replace('}', '_')
				.Replace('<', '_').Replace('>', '_');

			var fieldType = this.BuildFieldType(field.Value, sheet);
			fieldType.Name = name;
			type.AddField(fieldType);
		}

		return new FieldType() {
			ResolvedType = type,
			// TODO: Resolver to pass down the parser I guess?
			Resolver = new FuncFieldResolver<object>(context => context.Source)
		};
	}

	private FieldType BuildScalarFieldType(ScalarNode node, ISheetReader sheet) {
		// If the node type wasn't provided by the definition, check the reader
		// TODO: If this needs doing in 2+ places, may be better off doing a one-off hydrate per sheet instance.
		var columnType = node.Type == ScalarType.Unknown
			? sheet.GetColumn(node.Index).Type
			: node.Type;

		// If it's an unknown type, we shortcut with an explicit unknown handler
		if (columnType == ScalarType.Unknown) {
			return new FieldType() {
				ResolvedType = new StringGraphType(),
				Resolver = new FuncFieldResolver<object>(context => "UNKNOWN TYPE"),
			};
		}

		ScalarGraphType graphType = columnType switch {
			ScalarType.String => new StringGraphType(),
			ScalarType.Boolean => new BooleanGraphType(),
			ScalarType.Int8 => new SByteGraphType(),
			ScalarType.UInt8 => new ByteGraphType(),
			ScalarType.Int16 => new ShortGraphType(),
			ScalarType.UInt16 => new UShortGraphType(),
			ScalarType.Int32 => new IntGraphType(),
			ScalarType.UInt32 => new UIntGraphType(),
			ScalarType.Int64 => new LongGraphType(),
			ScalarType.UInt64 => new ULongGraphType(),
			ScalarType.Float => new FloatGraphType(),
			_ => throw new NotImplementedException(),
		};

		return new FieldType() {
			ResolvedType = graphType,
			// TODO: clean this up
			Resolver = new FuncFieldResolver<object>(context => {
				var rowReader = (IRowReader?)context.Source;
				return rowReader?.Read(node);
			}),
		};
	}
}

public class GraphQLDotNetSchema : IGraphQLSchema {
	private Schema schema;

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
		});
	}
}
