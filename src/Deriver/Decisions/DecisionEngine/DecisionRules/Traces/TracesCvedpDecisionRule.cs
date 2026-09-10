namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces;

public sealed class TracesCvedpDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        // temp until level 2 and 3 are implemented
        context.Level3Succeeded = true;
        return context.Ched?.ExchangedDocument.DocumentStatusCode switch
        {
            TracesNotificationStatus.Validated => new DecisionEngineResult(
                DecisionCode.C03,
                nameof(TracesChedppDecisionRule)
            ),
            _ => new DecisionEngineResult(
                DecisionCode.H01,
                nameof(TracesCvedpDecisionRule),
                DecisionInternalFurtherDetail.E99
            ),
        };
    }
}
