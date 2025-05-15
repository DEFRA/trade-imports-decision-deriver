using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Comparers;

public class ClearanceDecisionItemComparer : IEqualityComparer<ClearanceDecisionItem>
{
    public static readonly ClearanceDecisionItemComparer Default = new();

    public bool Equals(ClearanceDecisionItem? x, ClearanceDecisionItem? y)
    {
        if (ReferenceEquals(x, y))
            return true;
        if (x is null)
            return false;
        if (y is null)
            return false;
        return x.ItemNumber == y.ItemNumber
            && x.Checks.OrderBy(x => x.CheckCode)
                .SequenceEqual(y.Checks.OrderBy(x => x.CheckCode), ClearanceDecisionCheckComparer.Default);
    }

    public int GetHashCode(ClearanceDecisionItem obj)
    {
        throw new NotSupportedException();
    }
}
