using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Comparers;

public class ClearanceDecisionCheckComparer : IEqualityComparer<ClearanceDecisionCheck>
{
    public static readonly ClearanceDecisionCheckComparer Default = new();

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
