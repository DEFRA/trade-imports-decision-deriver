namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces;

public sealed class TracesChedppDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        return context.Ched?.ExchangedDocument.NotificationStatusCode switch
        {
            TracesNotificationStatus.Validated => new DecisionEngineResult(
                DecisionCode.C03,
                nameof(TracesChedppDecisionRule)
            ),
            _ => new DecisionEngineResult(
                DecisionCode.H01,
                nameof(TracesChedppDecisionRule),
                DecisionInternalFurtherDetail.E99
            ),
        };
    }
}
