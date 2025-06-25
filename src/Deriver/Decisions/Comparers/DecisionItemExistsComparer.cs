using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Comparers;

/// <summary>
/// Only use when comparing decisions to determine if a new
/// decision should be persisted.
/// </summary>
public class DecisionItemExistsComparer : IEqualityComparer<ClearanceDecisionItem>
{
    public static readonly DecisionItemExistsComparer Default = new();

    public bool Equals(ClearanceDecisionItem? x, ClearanceDecisionItem? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        if (x is null)
            return false;

        if (y is null)
            return false;

        return x.ItemNumber == y.ItemNumber
            && x.Checks.OrderBy(check => check.CheckCode)
                .SequenceEqual(y.Checks.OrderBy(check => check.CheckCode), DecisionItemCheckExistsComparer.Default);
    }

    public int GetHashCode(ClearanceDecisionItem obj)
    {
        throw new NotSupportedException();
    }
}
