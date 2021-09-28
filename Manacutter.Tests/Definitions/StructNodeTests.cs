using Manacutter.Definitions;
using System.Linq;
using Xunit;

namespace Manacutter.Tests.Definitions;

public class StructNodeTests {
	[Theory]
	[InlineData(1)]
	[InlineData(5)]
	public void CountSize(uint count) {
		var fields = Enumerable
			.Range(0, (int)count)
			.ToDictionary(
				value => value.ToString(),
				_ => new ScalarNode() as DefinitionNode
			);

		var structNode = new StructNode(fields);

		Assert.Equal(count, structNode.Size);
	}
}
