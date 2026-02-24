using System.Runtime.CompilerServices;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public sealed class AmendDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        var notification = context.Notification;

        if (notification.Status != ImportNotificationStatus.Amend)
        {
            return next(context);
        }

        if (notification.InspectionRequired is InspectionRequired.NotRequired or InspectionRequired.Inconclusive)
        {
            return DecisionEngineResult.H01E80;
        }

        return IsInspectionRequired(notification) ? DecisionEngineResult.H02E80 : DecisionEngineResult.H01E88;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsInspectionRequired(DecisionImportPreNotification notification)
    {
        return notification.InspectionRequired == InspectionRequired.Required
            || notification.Commodities.Any(x => x.HmiDecision == Constants.Required)
            || notification.Commodities.Any(x => x.PhsiDecision == CommodityRiskResultPhsiDecision.Required);
    }
}
