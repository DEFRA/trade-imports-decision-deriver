namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine.DecisionRules;

public sealed class AmendDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionResolutionContext context, DecisionRuleDelegate next)
    {
        var notification = context.Notification;

        if (notification.Status != ImportNotificationStatus.Amend)
        {
            return next(context);
        }

        if (notification.InspectionRequired is InspectionRequired.NotRequired or InspectionRequired.Inconclusive)
        {
            return new DecisionEngineResult(DecisionCode.H01, DecisionInternalFurtherDetail.E80);
        }

        return IsInspectionRequired(notification)
            ? new DecisionEngineResult(DecisionCode.H02, DecisionInternalFurtherDetail.E80)
            : new DecisionEngineResult(DecisionCode.H01, DecisionInternalFurtherDetail.E99);
    }

    private static bool IsInspectionRequired(DecisionImportPreNotification notification)
    {
        return notification.InspectionRequired == InspectionRequired.Required
            || notification.Commodities.Any(x => x.HmiDecision == CommodityRiskResultHmiDecision.Required)
            || notification.Commodities.Any(x => x.PhsiDecision == CommodityRiskResultPhsiDecision.Required);
    }
}
