using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

public abstract class DecisionFinder : IDecisionFinder
{
    protected abstract DecisionFinderResult FindDecisionInternal(
        DecisionImportPreNotification notification,
        CheckCode? checkCode
    );

    public virtual Type FinderType => GetType();
    public abstract string ChedType { get; }

    public abstract bool CanFindDecision(
        DecisionImportPreNotification notification,
        CheckCode? checkCode,
        string? documentCode
    );

    public DecisionFinderResult FindDecision(
        DecisionImportPreNotification notification,
        Commodity commodity,
        CheckCode? checkCode
    )
    {
        if (notification.ImportNotificationType != ChedType)
        {
            return new DecisionFinderResult(
                DecisionCode.X00,
                checkCode,
                InternalDecisionCode: DecisionInternalFurtherDetail.E84
            );
        }

        if (!notification.HasPartTwo)
        {
            return new DecisionFinderResult(
                DecisionCode.H01,
                checkCode,
                InternalDecisionCode: DecisionInternalFurtherDetail.E88
            );
        }

        var result = notification.Status switch
        {
            ImportNotificationStatus.Amend => GetAmendResult(notification, checkCode),
            ImportNotificationStatus.Cancelled => new DecisionFinderResult(
                DecisionCode.X00,
                checkCode,
                InternalDecisionCode: DecisionInternalFurtherDetail.E71
            ),
            ImportNotificationStatus.Replaced => new DecisionFinderResult(
                DecisionCode.X00,
                checkCode,
                InternalDecisionCode: DecisionInternalFurtherDetail.E72
            ),
            ImportNotificationStatus.Deleted => new DecisionFinderResult(
                DecisionCode.X00,
                checkCode,
                InternalDecisionCode: DecisionInternalFurtherDetail.E73
            ),
            ImportNotificationStatus.SplitConsignment => new DecisionFinderResult(
                DecisionCode.X00,
                checkCode,
                InternalDecisionCode: DecisionInternalFurtherDetail.E75
            ),
            _ => FindDecisionInternal(notification, checkCode),
        };

        return result;
    }

    protected static bool TryGetHoldDecision(DecisionImportPreNotification notification, out DecisionCode? decisionCode)
    {
        if (
            notification.Status
            is ImportNotificationStatus.Submitted
                or ImportNotificationStatus.InProgress
                or ImportNotificationStatus.Amend
        )
        {
            if (notification.InspectionRequired is InspectionRequired.NotRequired or InspectionRequired.Inconclusive)
            {
                decisionCode = DecisionCode.H01;
                return true;
            }

            if (IsInspectionRequired(notification))
            {
                decisionCode = DecisionCode.H02;
                return true;
            }
        }

        decisionCode = null;
        return false;
    }

    private static bool IsInspectionRequired(DecisionImportPreNotification notification)
    {
        return notification.InspectionRequired == InspectionRequired.Required
            || notification.Commodities.Any(x => x.HmiDecision == CommodityRiskResultHmiDecision.Required)
            || notification.Commodities.Any(x => x.PhsiDecision == CommodityRiskResultPhsiDecision.Required);
    }

    protected static DecisionFinderResult GetAmendResult(
        DecisionImportPreNotification notification,
        CheckCode? checkCode
    )
    {
        if (notification.InspectionRequired is InspectionRequired.NotRequired or InspectionRequired.Inconclusive)
        {
            return new DecisionFinderResult(DecisionCode.H01, checkCode, DecisionInternalFurtherDetail.E80);
        }

        return IsInspectionRequired(notification)
            ? new DecisionFinderResult(DecisionCode.H02, checkCode, DecisionInternalFurtherDetail.E80)
            : new DecisionFinderResult(DecisionCode.H01, checkCode, DecisionInternalFurtherDetail.E99);
    }
}
