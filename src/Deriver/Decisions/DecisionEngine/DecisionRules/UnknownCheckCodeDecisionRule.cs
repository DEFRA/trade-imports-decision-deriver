using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public sealed class UnknownCheckCodeDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        return new DecisionEngineResult(DecisionCode.X00, DecisionInternalFurtherDetail.E88);
    }
}
