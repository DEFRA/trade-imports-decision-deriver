using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public sealed class TerminalStatusDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        var notification = context.Notification;

        var result = notification.Status switch
        {
            ImportNotificationStatus.Cancelled => new DecisionEngineResult(
                DecisionCode.X00,
                DecisionInternalFurtherDetail.E71
            ),
            ImportNotificationStatus.Replaced => new DecisionEngineResult(
                DecisionCode.X00,
                DecisionInternalFurtherDetail.E72
            ),
            ImportNotificationStatus.Deleted => new DecisionEngineResult(
                DecisionCode.X00,
                DecisionInternalFurtherDetail.E73
            ),
            ImportNotificationStatus.SplitConsignment => new DecisionEngineResult(
                DecisionCode.X00,
                DecisionInternalFurtherDetail.E75
            ),
            _ => null,
        };

        return result ?? next(context);
    }
}
