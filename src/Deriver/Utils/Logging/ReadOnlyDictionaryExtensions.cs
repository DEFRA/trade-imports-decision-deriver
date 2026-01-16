using SlimMessageBus.Host;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Utils.Logging;

public static class ReadOnlyDictionaryExtensions
{
    public static string? GetContentEncoding(this IReadOnlyDictionary<string, object> headers)
    {
        return headers.TryGetValue("Content-Encoding", out var contentEncoding) ? contentEncoding.ToString() : null;
    }

    ////public static string? GetTraceId(this IReadOnlyDictionary<string, object> headers, string traceHeader)
    ////{
    ////    return headers.TryGetValue(traceHeader, out var traceId) ? traceId.ToString()?.Replace("-", "") : null;
    ////}
}
