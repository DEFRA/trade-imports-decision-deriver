namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine.DecisionRules;

public sealed class UnlinkedNotificationDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionResolutionContext context, DecisionRuleDelegate next)
    {
        if (context.Notification is null)
        {
            return DecisionEngineResult.Unlinked;
        }

        return next(context);
    }
}
