namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces;

public sealed class TracesTerminalStatusDecisionRule : TerminalStatusDecisionRule
{
    public override DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        return ExecuteInternal(context.Ched?.ExchangedDocument.NotificationStatusCode!, context, next);
    }
}
