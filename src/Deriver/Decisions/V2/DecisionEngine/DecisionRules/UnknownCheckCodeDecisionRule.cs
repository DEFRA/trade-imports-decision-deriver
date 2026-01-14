namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine.DecisionRules;

public sealed class UnknownCheckCodeDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionResolutionContext context, DecisionRuleDelegate next)
    {
        return new DecisionEngineResult(DecisionCode.X00, DecisionInternalFurtherDetail.E88);
    }
}