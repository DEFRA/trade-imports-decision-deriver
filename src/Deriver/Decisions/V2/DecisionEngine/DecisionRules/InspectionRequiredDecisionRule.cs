namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine.DecisionRules;

public sealed class InspectionRequiredDecisionRule : IDecisionRule
{
    public DecisionResolutionResult Execute(DecisionResolutionContext context, DecisionRuleDelegate next)
    {
        var notification = context.Notification;

        if (notification.Status is not (ImportNotificationStatus.Submitted or ImportNotificationStatus.InProgress))
        {
            return next(context);
        }

        if (notification.InspectionRequired is InspectionRequired.NotRequired or InspectionRequired.Inconclusive)
        {
            return new DecisionResolutionResult(DecisionCode.H01);
        }

        if (IsInspectionRequired(notification))
        {
            return new DecisionResolutionResult(DecisionCode.H02);
        }

        return next(context);
    }

    private static bool IsInspectionRequired(DecisionImportPreNotification notification)
    {
        return notification.InspectionRequired == InspectionRequired.Required
            || notification.Commodities.Any(x => x.HmiDecision == CommodityRiskResultHmiDecision.Required)
            || notification.Commodities.Any(x => x.PhsiDecision == CommodityRiskResultPhsiDecision.Required);
    }
}
