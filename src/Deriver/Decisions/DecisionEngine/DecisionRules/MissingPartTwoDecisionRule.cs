namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public sealed class MissingPartTwoDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        if (!context.Notification.HasPartTwo)
        {
            return new DecisionEngineResult(
                DecisionCode.H01,
                nameof(MissingPartTwoDecisionRule),
                DecisionInternalFurtherDetail.E88
            );
        }

        return next(context);
    }
}
