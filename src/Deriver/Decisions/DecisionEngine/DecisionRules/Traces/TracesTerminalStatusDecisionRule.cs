namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces;

public sealed class TracesTerminalStatusDecisionRule : TerminalStatusDecisionRule
{
    public override DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        return context.Ched?.ExchangedDocument.NotificationStatusCode switch
        {
            TracesNotificationStatus.Cancelled => new DecisionEngineResult(
                DecisionCode.X00,
                nameof(TracesTerminalStatusDecisionRule),
                DecisionInternalFurtherDetail.E71
            ),
            TracesNotificationStatus.Replaced => new DecisionEngineResult(
                DecisionCode.X00,
                nameof(TracesTerminalStatusDecisionRule),
                DecisionInternalFurtherDetail.E72
            ),
            TracesNotificationStatus.Deleted => new DecisionEngineResult(
                DecisionCode.X00,
                nameof(TracesTerminalStatusDecisionRule),
                DecisionInternalFurtherDetail.E73
            ),
            TracesNotificationStatus.SplitConsignment => new DecisionEngineResult(
                DecisionCode.X00,
                nameof(TracesTerminalStatusDecisionRule),
                DecisionInternalFurtherDetail.E75
            ),
            _ => next(context),
        };
    }
}
