using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Comparers;

public class ClearanceDecisionResultExistsComparer : IEqualityComparer<ClearanceDecisionResult>
{
    public static readonly ClearanceDecisionResultExistsComparer Default = new();

    public bool Equals(ClearanceDecisionResult? x, ClearanceDecisionResult? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        if (x is null || y is null)
            return false;

        return x.ItemNumber == y.ItemNumber
            && x.ImportPreNotification == y.ImportPreNotification
            && x.DocumentReference == y.DocumentReference
            && x.CheckCode == y.CheckCode
            && x.DecisionCode == y.DecisionCode
            && x.DecisionReason == y.DecisionReason
            && x.InternalDecisionCode == y.InternalDecisionCode;
    }

    public int GetHashCode(ClearanceDecisionResult obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        return HashCode.Combine(
            obj.ItemNumber,
            obj.ImportPreNotification,
            obj.DocumentReference,
            obj.CheckCode,
            obj.DecisionCode,
            obj.DecisionReason,
            obj.InternalDecisionCode
        );
    }
}
