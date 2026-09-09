namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces;

public sealed class TracesCedDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        return context.Ched?.ExchangedDocument.DocumentStatusCode switch
        {
            TracesNotificationStatus.Validated => next(context),
            _ => new DecisionEngineResult(DecisionCode.H01, nameof(CedDecisionRule), DecisionInternalFurtherDetail.E99),
        };
    }
}
