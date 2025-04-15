using Defra.TradeImportsDataApi.Domain.Ipaffs;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

public abstract class DecisionFinder : IDecisionFinder
{
    protected abstract DecisionFinderResult FindDecisionInternal(
        ImportPreNotification notification,
        CheckCode? checkCode
    );

    public abstract bool CanFindDecision(ImportPreNotification notification, CheckCode? checkCode);

    public DecisionFinderResult FindDecision(ImportPreNotification notification, CheckCode? checkCode)
    {
        if (
            notification.Status == ImportNotificationStatus.Cancelled
            || notification.Status == ImportNotificationStatus.Replaced
            || notification.Status == ImportNotificationStatus.Deleted
        )
        {
            return new DecisionFinderResult(
                DecisionCode.X00,
                checkCode,
                InternalDecisionCode: DecisionInternalFurtherDetail.E88
            );
        }

        return FindDecisionInternal(notification, checkCode);
    }

    protected static bool TryGetHoldDecision(ImportPreNotification notification, out DecisionCode? decisionCode)
    {
        if (notification.Status is ImportNotificationStatus.Submitted or ImportNotificationStatus.InProgress)
        {
            if (
                notification.PartTwo?.InspectionRequired
                is InspectionRequired.NotRequired
                    or InspectionRequired.Inconclusive
            )
            {
                decisionCode = DecisionCode.H01;
                return true;
            }

            if (
                notification.PartTwo?.InspectionRequired == InspectionRequired.Required
                || notification.Commodities.Any(x =>
                    x.RiskAssesment?.HmiDecision == CommodityRiskResultHmiDecision.Required
                )
                || notification.Commodities.Any(x =>
                    x.RiskAssesment?.PhsiDecision == CommodityRiskResultPhsiDecision.Required
                )
            )
            {
                decisionCode = DecisionCode.H02;
                return true;
            }
        }

        decisionCode = null;
        return false;
    }

    protected static DecisionFinderResult HandleNullNotAcceptableAction(
        ImportPreNotification notification,
        CheckCode? checkCode
    )
    {
        if (notification.PartTwo?.Decision?.NotAcceptableReasons?.Length > 0)
        {
            return new DecisionFinderResult(DecisionCode.N04, checkCode);
        }

        return new DecisionFinderResult(
            DecisionCode.X00,
            checkCode,
            InternalDecisionCode: DecisionInternalFurtherDetail.E97
        );
    }
}
