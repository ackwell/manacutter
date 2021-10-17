using Manacutter.Common.Schema;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Manacutter.Tests.Definitions;

internal class DefinitionWalkerStub : SchemaWalker<SchemaWalkerContext, string> {
	public override string VisitSheets(SheetsNode node, SchemaWalkerContext context) {
		var sheets = this.WalkSheets(node, context)
			.Select(pair => $"{pair.Key}:{pair.Value}");
		return $"{{{string.Join(',', sheets)}}}";
	}

	public override string VisitStruct(StructNode node, SchemaWalkerContext context) {
		var fields = this.WalkStruct(node, context)
			.Select(pair => $"{pair.Key}:{pair.Value}");
		return $"{{{string.Join(',', fields)}}}";
	}

	public override string VisitArray(ArrayNode node, SchemaWalkerContext context) {
		return $"{this.WalkArray(node, context)}[{node.Count}]";
	}

	public override string VisitReference(ReferenceNode node, SchemaWalkerContext context) {
		return $"ref|{string.Join(',', node.Targets.Select(target => target.Target))}";
	}

	public override string VisitScalar(ScalarNode node, SchemaWalkerContext context) {
		return "scalar";
	}
}

public class DefinitionWalkerTests {
	private DefinitionWalkerStub walker;

	public DefinitionWalkerTests() {
		walker = new DefinitionWalkerStub();
	}

	[Fact]
	public void VisitScalar() {
		var scalarNode = new ScalarNode();

		Assert.Equal(
			"scalar",
			walker.Visit(scalarNode, new SchemaWalkerContext())
		);
	}

	[Fact]
	public void VisitReference() {
		var referenceNode = new ReferenceNode() {
			Targets = new List<ReferenceTarget>() {
				new ReferenceTarget("Target1"),
				new ReferenceTarget("Target2") {
					Condition = new ReferenceTargetCondition() {
						FieldOffset = -1,
						Value = 1
					}
				},
			},
		};

		Assert.Equal(
			"ref|Target1,Target2",
			walker.Visit(referenceNode, new SchemaWalkerContext())
		);
	}

	[Fact]
	public void VisitArray() {
		var scalarNode = new ScalarNode();
		var arrayNode = new ArrayNode(scalarNode, 1);

		Assert.Equal(
			"scalar[1]",
			walker.Visit(arrayNode, new SchemaWalkerContext())
		);
	}

	[Fact]
	public void VisitStruct() {
		var scalarNode = new ScalarNode();
		var structNode = new StructNode(new Dictionary<string, (uint, SchemaNode)>() {
			{ "1", (0, scalarNode) },
			{ "2", (1, scalarNode) }
		});

		Assert.Equal(
			"{1:scalar,2:scalar}",
			walker.Visit(structNode, new SchemaWalkerContext())
		);
	}

	[Fact]
	public void VisitSheets() {
		var scalarNode = new ScalarNode();
		var structNode = new SheetsNode(new Dictionary<string, SchemaNode>() {
			{ "1", scalarNode },
			{ "2", scalarNode }
		});

		Assert.Equal(
			"{1:scalar,2:scalar}",
			walker.Visit(structNode, new SchemaWalkerContext())
		);
	}

	[Fact]
	public void VisitComplex() {
		var rootNode = new SheetsNode(new Dictionary<string, SchemaNode>() {
			{ "test", new StructNode(new Dictionary<string, (uint, SchemaNode)>() {
				{ "1", (0, new ScalarNode()) },
				{ "2", (1, new ArrayNode(
					new StructNode(new Dictionary<string, (uint, SchemaNode)>() {
						{ "3", (0, new ScalarNode()) }
					}),
					5
				)) },
				{ "4", (6, new ReferenceNode() {
					Targets = new List<ReferenceTarget>() { new ReferenceTarget("Target") },
				}) },
			}) }
		});

		Assert.Equal(
			"{test:{1:scalar,2:{3:scalar}[5],4:ref|Target}}",
			walker.Visit(rootNode, new SchemaWalkerContext())
		);
	}
}
