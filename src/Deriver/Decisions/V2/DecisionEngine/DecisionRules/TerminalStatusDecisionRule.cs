namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine.DecisionRules;

public sealed class TerminalStatusDecisionRule : IDecisionRule
{
    public DecisionResolutionResult Execute(DecisionResolutionContext context, DecisionRuleDelegate next)
    {
        var notification = context.Notification;

        var result = notification.Status switch
        {
            ImportNotificationStatus.Cancelled => new DecisionResolutionResult(
                DecisionCode.X00,
                DecisionInternalFurtherDetail.E71
            ),
            ImportNotificationStatus.Replaced => new DecisionResolutionResult(
                DecisionCode.X00,
                DecisionInternalFurtherDetail.E72
            ),
            ImportNotificationStatus.Deleted => new DecisionResolutionResult(
                DecisionCode.X00,
                DecisionInternalFurtherDetail.E73
            ),
            ImportNotificationStatus.SplitConsignment => new DecisionResolutionResult(
                DecisionCode.X00,
                DecisionInternalFurtherDetail.E75
            ),
            _ => null,
        };

        return result ?? next(context);
    }
}
