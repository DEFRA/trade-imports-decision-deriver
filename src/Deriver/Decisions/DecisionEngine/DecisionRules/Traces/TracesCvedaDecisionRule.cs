namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces;

public sealed class TracesCvedaDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        return new DecisionEngineResult(DecisionCode.C07, nameof(CvedaDecisionRule));
    }
}
