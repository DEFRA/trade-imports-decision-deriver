using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Comparers;

/// <summary>
/// Only use when comparing decisions to determine if a new
/// decision should be persisted.
/// </summary>
public class DecisionItemCheckExistsComparer : IEqualityComparer<ClearanceDecisionCheck>
{
    public static readonly DecisionItemCheckExistsComparer Default = new();

    public bool Equals(ClearanceDecisionCheck? x, ClearanceDecisionCheck? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        if (x is null)
            return false;

        if (y is null)
            return false;

        return x.CheckCode == y.CheckCode && x.DecisionCode == y.DecisionCode;
    }

    public int GetHashCode(ClearanceDecisionCheck obj)
    {
        throw new NotSupportedException();
    }
}
