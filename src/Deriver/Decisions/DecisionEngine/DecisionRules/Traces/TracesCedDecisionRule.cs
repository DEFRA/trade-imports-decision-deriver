namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces;

public sealed class TracesCedDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        return context.Ched?.ExchangedDocument.NotificationStatusCode switch
        {
            TracesNotificationStatus.Validated => new DecisionEngineResult(DecisionCode.C03, nameof(CedDecisionRule)),
            _ => new DecisionEngineResult(DecisionCode.H01, nameof(CedDecisionRule), DecisionInternalFurtherDetail.E99),
        };
    }
}
