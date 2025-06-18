using Defra.TradeImportsDecisionDeriver.Deriver.Utils.CorrelationId;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Extensions;

public class CorrelationIdTests
{
    [Fact]
    public void CorrelationId_ShouldBeGenerated()
    {
        var generator = new CorrelationIdGenerator();

        var id = generator.Generate();

        id.Length.Should().Be(20);
    }
}
