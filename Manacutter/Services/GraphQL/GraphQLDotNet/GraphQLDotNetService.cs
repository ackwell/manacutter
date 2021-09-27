﻿using GraphQL.Types;
using Manacutter.Services.Definitions;
using Manacutter.Services.Readers;
using System.Collections.Immutable;

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
		var sheetNames = new[] { "action", "item" };
		var builder = new FieldBuilder();

		var graphType = new ObjectGraphType() { Name = "Query" };

		foreach (var sheetName in sheetNames) {
			var sheet = this.reader.GetSheet(sheetName);
			if (sheet is null) { continue; }
			var sheetNode = definitionProvider.GetRootNode(sheetName);

			// Build the core field type for the sheet
			// TODO: I feel that the ID fields should be a concern of the field builder. Consider moving them in.
			var fieldType = builder.Visit(sheetNode, new FieldBuilderContext(sheet) {
				Path = ImmutableList.Create(sheetName)
			});
			if (fieldType.ResolvedType is not null) {
				fieldType.ResolvedType.Name = sheetName;
				this.AddIDFields(fieldType.ResolvedType);
			}

			// Add query fields to the root schema type
			graphType.AddField(new SingularSheetFieldType(fieldType, sheet));
			graphType.AddField(new PluralSheetFieldType(fieldType, sheet));
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
}
