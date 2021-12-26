using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using Manacutter.Common.Schema;
using Manacutter.Readers;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using GraphQL.Relay.Types;
using Humanizer;

namespace Manacutter.Services.GraphQL.GraphQLDotNet;

public record FieldBuilderContext : SchemaWalkerContext {
	public ImmutableList<string> Path { get; init; } = ImmutableList<string>.Empty;
}

public class FieldBuilder : SchemaWalker<FieldBuilderContext, FieldType> {
	private static readonly char IdSeparator = '/';
	private static string SanitizeName(string name) {
		// TODO: Some of this is very saint-specific, isolate?
		// Replace common symbols, then remove any remaining non-word symbols.
		name = name.Replace("%", "Percent");
		name = Regex.Replace(name, @"\W", "");

		// If there's a leading number, translate it to an english word representation.
		var leadingNumbers = name.TakeWhile(c => char.IsDigit(c)).ToArray();
		if (leadingNumbers.Length > 0) {
			var asWords = int.Parse(new string(leadingNumbers)).ToWords();
			name = $"{asWords}{name[leadingNumbers.Length..]}";
		}

		// We're splitting -> lowering before swapping to camel so that strings such as "AOZArrangement" become "aozArrangement" rather than "aOZArrangement".
		name = name.Humanize().ToLowerInvariant().Camelize();

		return name;
	}

	private readonly IReader reader;

	private Dictionary<string, IObjectGraphType> sheetRowsTypeCache = new();

	public FieldBuilder(
		IReader reader
	) : base() {
		this.reader = reader;
	}

	public override FieldType VisitSheets(SheetsNode node, FieldBuilderContext context) {
		// TODO: A bunch of stuff in this method assumes it's always the root - consider?
		var graphType = new ObjectGraphType() { Name = "Sheets" };

		foreach (var (name, field) in this.WalkSheets(node, context, (context, name, _) => context with {
			Path = ImmutableList.Create(SanitizeName(name)),
		})) {
			var sheet = this.reader.GetSheet(name);
			if (sheet is null) { continue; }

			if (field.ResolvedType is not null) {
				field.ResolvedType = this.AddIDFields(field.ResolvedType, sheet);
			}

			graphType.AddField(new SingularSheetFieldType(field, sheet));
			graphType.AddField(new PluralSheetFieldType(field, sheet));
		}

		return new FieldType() {
			Name = "Sheets",
			ResolvedType = new NonNullGraphType(graphType),
			Resolver = new FuncFieldResolver<object>(context => context.Source!)
		};
	}

	// TODO: Rename
	private IGraphType AddIDFields(IGraphType graphType, ISheetReader sheet) {
		if (graphType is not ObjectGraphType) {
			return graphType;
		}

		var objectGraphType = (ObjectGraphType)graphType;

		// TODO: This might be better off as a wrapper class akin to SingularSheetFieldType &c.
		// Build the core node graph type we'll use for the sheet.
		var nodeGraphType = new DefaultNodeGraphType<ExecutionContext, object?>(rowIdString => {
			// Resolve the ID string into the row ID, and subrow if this sheet supports them.
			uint? subRowId = null;
			if (sheet.HasSubrows) {
				// TODO: extract seperator into static
				var split = rowIdString.Split(IdSeparator);
				rowIdString = split[0];
				subRowId = uint.Parse(split[1]);
			}
			var rowId = uint.Parse(rowIdString);

			// Build the execution context for the node.
			var row = sheet.GetRow(rowId, subRowId);
			if (row is null) { return null; }
			return new ExecutionContext() { Row = row, GraphNodeName = graphType.Name };
		}) {
			Name = graphType.Name,
			IsTypeOf = input => {
				var execContext = input as ExecutionContext;
				if (execContext is null) { return false; }
				// The graph node name is only used when resolving node types - if it's not set, we arrived here through other means that we can assume are correct.
				if (execContext.GraphNodeName is null) { return true; }
				return graphType.Name == execContext.GraphNodeName;
			},
		};

		// Add the relay-compliant ID field. We're doing this manually rather than using .Id due to the latter's API not permitting the degree of customisation we need here.
		nodeGraphType.Field<NonNullGraphType<IdGraphType>>("id", resolve: context => {
			var row = context.Source!.Row;
			var id = $"{row?.RowID ?? 0}";
			if (sheet.HasSubrows) {
				id = $"{id}{IdSeparator}{row?.SubRowID ?? 0}";
			}
			return Node.ToGlobalId(context.ParentType.Name, id);
		});

		// Bring all the graph type's fields across to the new one.
		foreach (var field in objectGraphType.Fields) {
			// If there's an ID field, it'll conflict with the relay ID - prefix it with the type's name.
			if (field.Name.ToLowerInvariant() == "id") {
				field.Name = $"{graphType.Name}Id";
			}
			nodeGraphType.AddField(field);
		}

		// Add extra fields for the row's ID(s).
		nodeGraphType.Field<NonNullGraphType<UIntGraphType>>("rowId", resolve: context => {
			return context.Source?.Row?.RowID;
		});

		if (sheet.HasSubrows) {
			nodeGraphType.Field<NonNullGraphType<UIntGraphType>>("subRowId", resolve: context => {
				return context.Source?.Row?.SubRowID;
			});
		}

		return nodeGraphType;
	}

	public override FieldType VisitReference(ReferenceNode node, FieldBuilderContext walkerContext) {
		// TODO: should probably use a direct ref type if ony one target
		// TODO: This name generation is common, should probably generalise it
		var union = new UnionGraphType() {
			Name = $"{string.Join('_', walkerContext.Path.Select(part => part.Pascalize()))}_UnionTest",
		};

		foreach (var target in node.Targets) {
			var sheetReader = this.reader.GetSheet(target.Sheet);
			if (sheetReader == null) { continue; }

			union.AddPossibleType(sheetReader.HasSubrows
				? this.BuildSheetRowsType(target.Sheet)
				: new GraphQLTypeReference(SanitizeName(target.Sheet).Pascalize()));
		}

		return new FieldType() {
			Name = walkerContext.Path.Last(),
			ResolvedType = union,
			Resolver = new FuncFieldResolver<object>(context => {
				// TODO: quite a lot of this is going to be shared with RESTBuilder, work out how much can be combined. for now, comments on logic are over there.

				var executionContext = (ExecutionContext)context.Source!;
				if (executionContext.Row is null) {
					return null;
				}

				var targetRowId = Convert.ToInt32(executionContext.Row.ReadColumn(walkerContext.Offset + executionContext.Offset));

				// TODO: do we need depth checks, given this is GQL and ergo lazy?
				// TODO: should this be null?
				if (targetRowId < 0) {
					return null;
				}

				var conditionFieldCache = new Dictionary<string, uint>();

				foreach (var target in node.Targets) {
					if (target.Condition is not null) {
						var condition = target.Condition;

						if (!conditionFieldCache.TryGetValue(condition.Field, out var value)) {
							value = Convert.ToUInt32(this.GetFieldValue(walkerContext, context, condition.Field));
							conditionFieldCache[condition.Field] = value;
						}

						if (value != condition.Value) {
							continue;
						}
					}

					var sheetReader = this.reader.GetSheet(target.Sheet);
					if (sheetReader is null) { continue; }

					if (target.Field is not null) {
						throw new NotImplementedException();
					}

					if (sheetReader.HasSubrows) {
						var subRows = sheetReader.EnumerateRows((uint)targetRowId, null)
							.TakeWhile(reader => reader.RowID == targetRowId)
							.Select(reader => executionContext with { 
								Row = reader,
								GraphNodeName = SanitizeName(target.Sheet).Pascalize(),
								Offset = 0,
							});

						if (!subRows.Any()) {
							continue;
						}

						return subRows;
					}

					var rowReader = sheetReader.GetRow((uint)targetRowId);
					if (rowReader is null) { continue; }

					return executionContext with {
						Row = rowReader,
						GraphNodeName = SanitizeName(target.Sheet).Pascalize(),
						Offset = 0,
					};
				}

				return null;
			}),
		};
	}

	public override FieldType VisitStruct(StructNode node, FieldBuilderContext walkerContext) {
		var type = new ObjectGraphType() {
			Name = string.Join('_', walkerContext.Path.Select(part => part.Pascalize())),
		};

		foreach (var pair in this.WalkStruct(node, walkerContext, (context, name, _) => context with {
			Path = context.Path.Add(SanitizeName(name))
		})) {
			type.AddField(pair.Value);
		}

		return new FieldType() {
			Name = walkerContext.Path.Last(),
			ResolvedType = type,
			Resolver = new FuncFieldResolver<object>(context => {
				var executionContext = (ExecutionContext)context.Source!;
				return executionContext with { Offset = executionContext.Offset };
			})
		};
	}

	public override FieldType VisitArray(ArrayNode node, FieldBuilderContext walkerContext) {
		var fieldType = this.WalkArray(node, walkerContext);

		return new FieldType() {
			Name = walkerContext.Path.Last(),
			ResolvedType = new ListGraphType(fieldType.ResolvedType),
			Resolver = new FuncFieldResolver<object>(context => {
				var executionContext = (ExecutionContext)context.Source!;
				var baseOffset = executionContext.Offset;
				var elementWidth = node.Type.Size;

				var results = context.ArrayPool.Rent<object?>((int)node.Count);
				for (int index = 0; index < node.Count; index++) {
					var elementOffset = index * elementWidth;

					var newContext = new ResolveFieldContext<ExecutionContext>(context) {
						Source = executionContext with {
							Offset = (uint)(baseOffset + elementOffset)
						},
					};

					results[index] = fieldType.Resolver?.Resolve(newContext);
				}

				return results.Constrained((int)node.Count);
			}),
		};
	}

	public override FieldType VisitScalar(ScalarNode node, FieldBuilderContext walkerContext) {
		var fieldName = walkerContext.Path.Last();

		// If it's an unknown type, we shortcut with an explicit unknown handler
		if (node.Type == ScalarType.Unknown) {
			return new FieldType() {
				Name = fieldName,
				ResolvedType = new StringGraphType(),
				Resolver = new FuncFieldResolver<object>(context => "UNKNOWN TYPE"),
			};
		}

		ScalarGraphType graphType = node.Type switch {
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
			Name = fieldName,
			ResolvedType = graphType,
			Resolver = new FuncFieldResolver<object>(context => {
				var executionContext = (ExecutionContext)context.Source!;
				return executionContext.Row?.ReadColumn(walkerContext.Offset + executionContext.Offset);
			}),
		};
	}

	private IObjectGraphType BuildSheetRowsType(string sheet) {
		if (this.sheetRowsTypeCache.TryGetValue(sheet, out var graphType)) {
			// TODO: should this be a reference?
			return graphType;
		}

		// TODO: need to standardise sheet name -> type name generation for xrefs
		var sheetTypeName = SanitizeName(sheet).Pascalize();

		// NOTE: If we start getting a lot of references with large subrow counts, deprecate this in favor of using a connection type.
		graphType = new ObjectGraphType() {
			Name = $"{sheetTypeName}Rows",
			IsTypeOf = input => {
				var executionContexts = input as IEnumerable<ExecutionContext>;
				if (executionContexts is null) { return false; }
				var something = executionContexts.First();
				return something.GraphNodeName == sheetTypeName;
			}
		};

		graphType.AddField(new FieldType() {
			Name = "rows",
			ResolvedType = new ListGraphType(new GraphQLTypeReference(sheetTypeName)),
			Resolver = new FuncFieldResolver<object>(context => context.Source)
		});

		this.sheetRowsTypeCache[sheet] = graphType;

		return graphType;
	}

	private object? GetFieldValue(FieldBuilderContext walkerContext, IResolveFieldContext resolveContext, string field) {
		IResolveFieldContext? resolveParent = resolveContext;
		foreach (var walkerParent in walkerContext.EnumerateAncestors()) {
			if (walkerParent.Node is not StructNode node || !node.Fields.ContainsKey(field)) {
				resolveParent = resolveParent?.Parent;
				continue;
			}

			if (resolveParent is null) {
				break;
			}

			var executionParent = (ExecutionContext)resolveParent.Source!;

			var offset = walkerParent.Offset + executionParent.Offset + node.Fields[field].Offset;
			// TODO: We're using the immediate context for the row info which doesn't seem super stable... look into why using parent fails.
			return ((ExecutionContext)resolveContext.Source!).Row?.ReadColumn(offset);
		}

		return null;
	}
}
