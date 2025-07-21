using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Comparers;

/// <summary>
/// Only use when comparing decisions to determine if a new
/// decision should be persisted.
/// </summary>
public class DecisionExistsComparer : IEqualityComparer<ClearanceDecision>
{
    public static readonly DecisionExistsComparer Default = new();

    public bool Equals(ClearanceDecision? x, ClearanceDecision? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        if (x is null)
            return false;

        if (y is null)
            return false;

        return x.Items.OrderBy(item => item.ItemNumber)
                .SequenceEqual(y.Items.OrderBy(item => item.ItemNumber), DecisionItemExistsComparer.Default);
    }

    public int GetHashCode(ClearanceDecision obj)
    {
        throw new NotSupportedException();
    }
}
