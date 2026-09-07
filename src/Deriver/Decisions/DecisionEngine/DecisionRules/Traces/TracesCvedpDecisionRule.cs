namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces;

public sealed class TracesCvedpDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        return context.Ched?.ExchangedDocument.NotificationStatusCode switch
        {
            TracesNotificationStatus.Validated => new DecisionEngineResult(
                DecisionCode.C03,
                nameof(TracesCvedpDecisionRule)
            ),
            _ => new DecisionEngineResult(
                DecisionCode.H01,
                nameof(TracesCvedpDecisionRule),
                DecisionInternalFurtherDetail.E99
            ),
        };
    }
}
