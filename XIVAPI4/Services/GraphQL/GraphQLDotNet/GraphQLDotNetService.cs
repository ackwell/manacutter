using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.SystemTextJson;
using System.Text.Json;
using XIVAPI4.Services.Readers;
using XIVAPI4.Types;

namespace XIVAPI4.Services.GraphQL.GraphQLDotNet;

public class GraphQLDotNetService : IGraphQLService {
	private readonly IReader reader;

	public GraphQLDotNetService(
		IReader reader
	) {
		this.reader = reader;
	}

	// TODO: Not sure about these args, we'll possibly need more data to feed into the reader
	public IGraphQLSchema BuildSchema(string sheetName, DataNode node) {
		// TODO: sheet name needs standardisation across the board
		var sheet = this.reader.GetSheet(sheetName);
		// TODO: null check sheet

		// TODO: how do we go from field type to something representing a sheet?
		// possibly pull out the resolver and replace with a copy that calls down?
		var fieldType = this.BuildFieldType(node, sheet);

		// temp beneath here
		fieldType.ResolvedType!.Name = sheetName;

		// TODO: might want to make a .Copy extension method?
		var singular = new FieldType() {
			Name = sheetName,
			ResolvedType = fieldType.ResolvedType,
			Arguments = new QueryArguments(
				new QueryArgument<NonNullGraphType<UIntGraphType>>() { Name = "id" }	
			),
			Resolver = new FuncFieldResolver<object>(context => {
				// TODO: we really need to call down to the base field type, data should be moving around in a class we control
				var id = context.GetArgument<uint>("id");
				return sheet.GetRow(id);
				//return fieldType.Resolver?.Resolve(context);
			})
		};

		var graphType = new ObjectGraphType();
		graphType.Name = "Query";
		graphType.AddField(singular);

		return new GraphQLDotNetSchema(graphType, sheet);
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
	// TODO: this is temp. sheet fields should be doing... something... to this tune internally.
	private ISheetReader sheet;

	public GraphQLDotNetSchema(
		ObjectGraphType rootGraphType,
		ISheetReader sheet
	) {
		this.schema = new Schema() {
			Query = rootGraphType
		};
		this.sheet = sheet;
	}

	// TODO: think about the variables type a bit.
	public Task<string> Query(string query, JsonElement variables) {
		return this.schema.ExecuteAsync(options => {
			options.Query = query;
			options.Inputs = variables.ToInputs();
			// TODO: lmao
			options.Root = this.sheet.GetRow(7518);
		});
	}
}
