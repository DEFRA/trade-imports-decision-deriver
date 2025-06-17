using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Extensions;

public class CorrelationIdTests
{
    [Fact]
    public void CorrelationId_WithTimestamp_ShouldBeGenerated()
    {
        var dateTime = new DateTimeOffset(2025, 08, 08, 05, 25, 44, TimeSpan.Zero);
        var timestamp = dateTime.ToUnixTimeMilliseconds();
        var id = CorrelationId.GenerateNewId(timestamp);

        id.CreationTime.Should().Be(dateTime);
        id.Timestamp.Should().Be(timestamp);
        id.ToString().Should().StartWith(timestamp.ToString());
    }

    [Fact]
    public void CorrelationId_WithNoTimestamp_ShouldBeGenerated()
    {
        var id = CorrelationId.GenerateNewId();

        id.ToString().Should().NotBeNull();
    }
}
