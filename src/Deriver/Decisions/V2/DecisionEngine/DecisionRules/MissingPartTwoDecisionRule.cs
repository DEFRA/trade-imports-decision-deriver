namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine.DecisionRules;

public sealed class MissingPartTwoDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionResolutionContext context, DecisionRuleDelegate next)
    {
        if (!context.Notification.HasPartTwo)
        {
            return new DecisionEngineResult(DecisionCode.H01, DecisionInternalFurtherDetail.E88);
        }

        return next(context);
    }
}
