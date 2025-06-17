namespace Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

public readonly struct CorrelationId
{
#pragma warning disable S2245
    private static readonly int __random = new Random().Next(1, 9999);
#pragma warning restore S2245
    // private fields
    private readonly long _timestamp;
    private readonly int _randomValue;

    private CorrelationId(long timestamp, int randomValue)
    {
        _timestamp = timestamp;
        _randomValue = randomValue;
    }

    public long Timestamp => _timestamp;

    public DateTimeOffset CreationTime => DateTimeOffset.FromUnixTimeMilliseconds(_timestamp);

    public override string ToString()
    {
        return $"{_timestamp}{_randomValue}";
    }

    public static CorrelationId GenerateNewId()
    {
        return GenerateNewId(GetTimestampFromDateTime(DateTimeOffset.Now));
    }

    public static CorrelationId GenerateNewId(long timestamp)
    {
        return new CorrelationId(timestamp, __random);
    }

    private static long GetTimestampFromDateTime(DateTimeOffset timestamp)
    {
        return timestamp.ToUnixTimeMilliseconds();
    }
}
