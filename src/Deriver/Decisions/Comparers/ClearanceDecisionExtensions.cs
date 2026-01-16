using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Comparers;

/// <summary>
/// Only use when comparing decisions to determine if a new
/// decision should be persisted.
/// </summary>
public static class ClearanceDecisionExtensions
{
    public static bool IsSameAs(this ClearanceDecision? x, ClearanceDecision? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        if (x is null || y is null)
            return false;

        if (x.ExternalVersionNumber != y.ExternalVersionNumber)
            return false;

        var xResults = x.Results;
        var yResults = y.Results;

        if (xResults is null || yResults is null)
            return xResults is null && yResults is null;

        if (xResults.Length != yResults.Length)
            return false;

        var comparer = ClearanceDecisionResultExistsComparer.Default;

        // Count occurrences instead of sorting
        var counts = new Dictionary<ClearanceDecisionResult, int>(comparer);

        foreach (var r in xResults)
        {
            counts.TryGetValue(r, out var count);
            counts[r] = count + 1;
        }

        foreach (var r in yResults)
        {
            if (!counts.TryGetValue(r, out var count))
                return false;

            if (count == 1)
                counts.Remove(r);
            else
                counts[r] = count - 1;
        }

        return counts.Count == 0;
    }
}
