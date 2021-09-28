using Manacutter.Definitions;
using Xunit;

namespace Manacutter.Tests.Definitions;

public class ArrayNodeTests {
	[Theory]
	[InlineData(1)]
	[InlineData(5)]
	public void CountSize(uint count) {
		var scalarNode = new ScalarNode();
		var arrayNode = new ArrayNode(scalarNode, count);

		Assert.Equal(count, arrayNode.Size);
	}
}
