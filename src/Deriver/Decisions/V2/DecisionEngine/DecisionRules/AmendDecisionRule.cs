namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine.DecisionRules;

public sealed class AmendDecisionRule : IDecisionRule
{
    public DecisionResolutionResult Execute(DecisionResolutionContext context, DecisionRuleDelegate next)
    {
        var notification = context.Notification;

        if (notification.Status != ImportNotificationStatus.Amend)
        {
            return next(context);
        }

        if (notification.InspectionRequired is InspectionRequired.NotRequired or InspectionRequired.Inconclusive)
        {
            return new DecisionResolutionResult(DecisionCode.H01, DecisionInternalFurtherDetail.E80);
        }

        return IsInspectionRequired(notification)
            ? new DecisionResolutionResult(DecisionCode.H02, DecisionInternalFurtherDetail.E80)
            : new DecisionResolutionResult(DecisionCode.H01, DecisionInternalFurtherDetail.E99);
    }

    private static bool IsInspectionRequired(DecisionImportPreNotification notification)
    {
        return notification.InspectionRequired == InspectionRequired.Required
            || notification.Commodities.Any(x => x.HmiDecision == CommodityRiskResultHmiDecision.Required)
            || notification.Commodities.Any(x => x.PhsiDecision == CommodityRiskResultPhsiDecision.Required);
    }
}
