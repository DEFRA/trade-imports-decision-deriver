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

        if (x is null)
            return false;

        if (y is null)
            return false;

        if (x.Results is null && y.Results is null)
            return true;

        if (x.Results is null && y.Results is not null)
            return false;

        if (x.Results is not null && y.Results is null)
            return false;

        ClearanceDecisionResult[] xResults = x.Results!;
        ClearanceDecisionResult[] yResults = y.Results!;

        return xResults
            .OrderBy(r => r.DocumentReference)
            .ThenBy(r => r.DocumentCode)
            .SequenceEqual(
                yResults.OrderBy(r => r.DocumentReference).ThenBy(r => r.DocumentCode),
                ClearanceDecisionResultExistsComparer.Default
            );
    }
}
