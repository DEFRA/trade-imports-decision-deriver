namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public sealed class TerminalStatusDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        var notification = context.Notification;

        return notification.Status switch
        {
            ImportNotificationStatus.Cancelled => new DecisionEngineResult(
                DecisionCode.X00,
                nameof(TerminalStatusDecisionRule),
                DecisionInternalFurtherDetail.E71
            ),
            ImportNotificationStatus.Replaced => new DecisionEngineResult(
                DecisionCode.X00,
                nameof(TerminalStatusDecisionRule),
                DecisionInternalFurtherDetail.E72
            ),
            ImportNotificationStatus.Deleted => new DecisionEngineResult(
                DecisionCode.X00,
                nameof(TerminalStatusDecisionRule),
                DecisionInternalFurtherDetail.E73
            ),
            ImportNotificationStatus.SplitConsignment => new DecisionEngineResult(
                DecisionCode.X00,
                nameof(TerminalStatusDecisionRule),
                DecisionInternalFurtherDetail.E75
            ),
            _ => next(context),
        };
    }
}
