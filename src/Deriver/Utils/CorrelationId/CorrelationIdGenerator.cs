namespace Defra.TradeImportsDecisionDeriver.Deriver.Utils.CorrelationId;

public class CorrelationIdGenerator : ICorrelationIdGenerator
{
    public string Generate()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = Random.Shared.Next(1, 9999);
        return $"{timestamp}{random}";
    }
}
