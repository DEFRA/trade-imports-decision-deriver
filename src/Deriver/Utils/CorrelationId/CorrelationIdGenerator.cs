namespace Defra.TradeImportsDecisionDeriver.Deriver.Utils.CorrelationId;

public class CorrelationIdGenerator : ICorrelationIdGenerator
{
    public string Generate()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = Random.Shared.Next(100000, 9999999);
        return $"{timestamp}{random}";
    }
}
