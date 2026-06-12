namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public sealed class UnknownChedTypeDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        return new DecisionEngineResult(
            DecisionCode.X00,
            nameof(UnknownChedTypeDecisionRule),
            DecisionInternalFurtherDetail.E81
        );
    }
}
