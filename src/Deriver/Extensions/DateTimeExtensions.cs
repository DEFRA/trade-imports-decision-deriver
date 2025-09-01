namespace Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

public static class DateTimeExtensions
{
    public static DateTime TrimMicroseconds(this DateTime dt)
    {
        return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond, dt.Kind);
    }

    public static DateTime? TrimMicroseconds(this DateTime? dt)
    {
        return dt?.TrimMicroseconds();
    }
}
