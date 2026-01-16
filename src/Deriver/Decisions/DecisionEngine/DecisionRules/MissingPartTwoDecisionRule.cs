using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public sealed class MissingPartTwoDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        if (!context.Notification.HasPartTwo)
        {
            return new DecisionEngineResult(DecisionCode.H01, DecisionInternalFurtherDetail.E88);
        }

        return next(context);
    }
}
