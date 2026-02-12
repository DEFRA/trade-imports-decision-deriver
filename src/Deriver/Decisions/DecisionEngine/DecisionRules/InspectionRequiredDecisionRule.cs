using System.Runtime.CompilerServices;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public sealed class InspectionRequiredDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        if (context.CheckCode.IsIuu())
        {
            return next(context);
        }

        var notification = context.Notification;

        if (notification.Status is not (ImportNotificationStatus.Submitted or ImportNotificationStatus.InProgress))
        {
            return next(context);
        }

        if (notification.InspectionRequired is InspectionRequired.NotRequired or InspectionRequired.Inconclusive)
        {
            return DecisionEngineResult.H01;
        }

        if (IsInspectionRequired(notification))
        {
            return DecisionEngineResult.H02;
        }

        return next(context);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsInspectionRequired(DecisionImportPreNotification notification)
    {
        return notification.InspectionRequired == InspectionRequired.Required
            || notification.Commodities.Any(x => x.HmiDecision == Constants.Required)
            || notification.Commodities.Any(x => x.PhsiDecision == CommodityRiskResultPhsiDecision.Required);
    }
}
