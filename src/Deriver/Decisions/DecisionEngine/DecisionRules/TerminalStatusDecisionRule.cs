namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public class TerminalStatusDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        var notification = context.Notification;

        return ExecuteInternal(notification.Status!, next);
    }

    protected DecisionEngineResult ExecuteInternal(string status, DecisionRuleDelegate next)
    {
        return status switch
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
            ImportNotificationStatus.Modify => new DecisionEngineResult(
                DecisionCode.H01,
                nameof(TerminalStatusDecisionRule),
                DecisionInternalFurtherDetail.E81
            ),
            _ => next(context),
        };
    }
}
