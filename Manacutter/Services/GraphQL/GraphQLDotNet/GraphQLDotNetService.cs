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
		var sheetNames = new[] { "Action", "Item" };
		var builder = new FieldBuilder();

		var graphType = new ObjectGraphType() { Name = "Query" };

		foreach (var sheetName in sheetNames) {
			var sheet = this.reader.GetSheet(sheetName);
			if (sheet is null) { continue; }
			var sheetNode = definitionProvider.GetRootNode(sheetName);

			// Build & name the core field type for the sheet
			//var fieldType = this.BuildFieldType(sheetNode, sheet, 0);
			var fieldType = builder.Visit(sheetNode, new FieldBuilderContext(sheet));
			fieldType.Name = sheetName;
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

	private FieldType BuildFieldType(DataNode node, ISheetReader sheet, uint offset) {
		return node switch {
			StructNode structNode => this.BuildStructFieldType(structNode, sheet, offset),
			ArrayNode arrayNode => this.BuildArrayFieldType(arrayNode, sheet, offset),
			ScalarNode scalarNode => this.BuildScalarFieldType(scalarNode, sheet, offset),
			_ => throw new NotImplementedException(),
		};
	}

	private FieldType BuildStructFieldType(StructNode node, ISheetReader sheet, uint offset) {
		var type = new ObjectGraphType();

		foreach (var field in node.Fields) {
			// TODO: lmao
			var name = field.Key
				.Replace('{', '_').Replace('}', '_')
				.Replace('<', '_').Replace('>', '_');

			var fieldType = this.BuildFieldType(field.Value, sheet, offset + node.Offset);
			fieldType.Name = name;
			// TODO: this should be prefixed with parent names to prevent cross-sheet collisions
			// TODO: this.. really should be in the return, not modified afterwards
			var childType = fieldType.ResolvedType?.GetNamedType();
			if (childType is ObjectGraphType) {
				childType.Name = name;
			}
			type.AddField(fieldType);
		}

		return new FieldType() {
			ResolvedType = type,
			Resolver = new FuncFieldResolver<object>(context => {
				var executionContext = (ExecutionContext)context.Source!;
				// TODO: might be worth making a copy method for this
				return new ExecutionContext() {
					Sheet = executionContext.Sheet,
					Row = executionContext.Row,
					Offset = executionContext.Offset + node.Offset,
				};
			})
		};
	}

	private FieldType BuildArrayFieldType(ArrayNode node, ISheetReader sheet, uint offset) {
		var fieldType = this.BuildFieldType(node.Type, sheet, offset + node.Offset);

		return new FieldType() {
			ResolvedType = new ListGraphType(fieldType.ResolvedType),
			Resolver = new FuncFieldResolver<object>(context => {
				var executionContext = (ExecutionContext)context.Source!;
				var baseOffset = executionContext.Offset + node.Offset;
				var elementWidth = node.Type.Size;

				var execContexts = context.ArrayPool.Rent<ExecutionContext>((int)node.Count);
				for (int index = 0; index < node.Count; index++) {
					var elementOffset = index * elementWidth;
					execContexts[index] = new ExecutionContext() {
						Sheet = executionContext.Sheet,
						Row = executionContext.Row,
						Offset = (uint)(baseOffset + elementOffset),
					};
				}

				return execContexts.Constrained((int)node.Count);
			}),
		};
	}

	private FieldType BuildScalarFieldType(ScalarNode node, ISheetReader sheet, uint offset) {
		// If the node type wasn't provided by the definition, check the reader
		// TODO: If this needs doing in 2+ places, may be better off doing a one-off hydrate per sheet instance.
		var columnType = node.Type == ScalarType.Unknown
			? sheet.GetColumn(offset + node.Offset).Type
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
			Resolver = new FuncFieldResolver<object>(context => {
				var execContext = (ExecutionContext)context.Source!;
				// This is not including the offset, as it's added by the reader
				// TODO: The above seems janky, in a way. Think about it.
				return execContext.Row?.Read(node, execContext.Offset + node.Offset);
			}),
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
