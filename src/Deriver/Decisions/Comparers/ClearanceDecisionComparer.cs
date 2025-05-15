using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Comparers;

public class ClearanceDecisionComparer : IEqualityComparer<ClearanceDecision>
{
    public static readonly ClearanceDecisionComparer Default = new();

    public bool Equals(ClearanceDecision? x, ClearanceDecision? y)
    {
        if (ReferenceEquals(x, y))
            return true;
        if (x is null)
            return false;
        if (y is null)
            return false;
        return x.SourceVersion == y.SourceVersion
            || x.Items.OrderBy(x => x.ItemNumber)
                .SequenceEqual(y.Items.OrderBy(x => x.ItemNumber), ClearanceDecisionItemComparer.Default);
    }

    public int GetHashCode(ClearanceDecision obj)
    {
        throw new NotSupportedException();
    }
}
