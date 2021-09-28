using Manacutter.Definitions;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Manacutter.Tests.Definitions;

internal class DefinitionWalkerStub : DefinitionWalker<DefinitionWalkerContext, string> {
	public override string VisitStruct(StructNode node, DefinitionWalkerContext context) {
		var fields = this.WalkStruct(node, context)
			.Select(pair => $"{pair.Key}:{pair.Value}");
		return $"{{{string.Join(',', fields)}}}";
	}

	public override string VisitArray(ArrayNode node, DefinitionWalkerContext context) {
		return $"{this.WalkArray(node, context)}[{node.Count}]";
	}

	public override string VisitScalar(ScalarNode node, DefinitionWalkerContext context) {
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
			walker.Visit(scalarNode, new DefinitionWalkerContext())
		);
	}

	[Fact]
	public void VisitArray() {
		var scalarNode = new ScalarNode();
		var arrayNode = new ArrayNode(scalarNode, 1);

		Assert.Equal(
			"scalar[1]",
			walker.Visit(arrayNode, new DefinitionWalkerContext())
		);
	}

	[Fact]
	public void VisitStruct() {
		var scalarNode = new ScalarNode();
		var structNode = new StructNode(new Dictionary<string, DefinitionNode>() {
			{ "1", scalarNode },
			{ "2", scalarNode }
		});

		Assert.Equal(
			"{1:scalar,2:scalar}",
			walker.Visit(structNode, new DefinitionWalkerContext())
		);
	}

	[Fact]
	public void VisitComplex() {
		var rootNode = new StructNode(new Dictionary<string, DefinitionNode>() {
			{ "1", new ScalarNode() },
			{ "2", new ArrayNode(
				new StructNode(new Dictionary<string, DefinitionNode>() {
					{ "3", new ScalarNode() }
				}),
				5
			)}
		});

		Assert.Equal(
			"{1:scalar,2:{3:scalar}[5]}",
			walker.Visit(rootNode, new DefinitionWalkerContext())
		);
	}
}
