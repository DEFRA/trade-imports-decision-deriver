namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public sealed class TerminalStatusDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        var notification = context.Notification;

        return notification.Status switch
        {
            ImportNotificationStatus.Cancelled => DecisionEngineResult.X00E71,
            ImportNotificationStatus.Replaced => DecisionEngineResult.X00E72,
            ImportNotificationStatus.Deleted => DecisionEngineResult.X00E73,
            ImportNotificationStatus.SplitConsignment => DecisionEngineResult.X00E75,
            _ => next(context),
        };
    }
}
