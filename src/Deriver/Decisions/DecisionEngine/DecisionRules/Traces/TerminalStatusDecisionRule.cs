namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces;

public sealed class TerminalStatusDecisionRule
    : Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.TerminalStatusDecisionRule
{
    public new DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        return ExecuteInternal(context.Ched?.ExchangedDocument.NotificationStatusCode!, next);
    }
}
