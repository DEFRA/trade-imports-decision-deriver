namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

public abstract class DecisionFinder : IDecisionFinder
{
    protected abstract DecisionFinderResult FindDecisionInternal(
        DecisionImportPreNotification notification,
        CheckCode? checkCode
    );

    protected abstract string ChedType { get; }

    public abstract bool CanFindDecision(
        DecisionImportPreNotification notification,
        CheckCode? checkCode,
        string? documentCode
    );

    public DecisionFinderResult FindDecision(DecisionImportPreNotification notification, CheckCode? checkCode)
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

        return notification.Status switch
        {
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
    }

    protected static bool TryGetHoldDecision(DecisionImportPreNotification notification, out DecisionCode? decisionCode)
    {
        if (notification.Status is ImportNotificationStatus.Submitted or ImportNotificationStatus.InProgress)
        {
            if (notification.InspectionRequired is InspectionRequired.NotRequired or InspectionRequired.Inconclusive)
            {
                decisionCode = DecisionCode.H01;
                return true;
            }

            if (
                notification.InspectionRequired == InspectionRequired.Required
                || notification.Commodities.Any(x => x.HmiDecision == CommodityRiskResultHmiDecision.Required)
                || notification.Commodities.Any(x => x.PhsiDecision == CommodityRiskResultPhsiDecision.Required)
            )
            {
                decisionCode = DecisionCode.H02;
                return true;
            }
        }

        decisionCode = null;
        return false;
    }
}
