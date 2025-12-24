using System.Diagnostics;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

public static class TimingExtensions
{
    public static async Task<(T Result, TimeSpan Elapsed)> TimeAsync<T>(Func<Task<T>> action)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await action().ConfigureAwait(false);
            return (result, stopwatch.Elapsed);
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    public static (T Result, TimeSpan Elapsed) Time<T>(this Func<T> action)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = action();
            return (result, stopwatch.Elapsed);
        }
        finally
        {
            stopwatch.Stop();
        }
    }
}
