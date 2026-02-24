namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public sealed class MissingPartTwoDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        if (!context.Notification.HasPartTwo)
        {
            return DecisionEngineResult.H01E88;
        }

        return next(context);
    }
}
