namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public sealed class UnlinkedNotificationDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        if (context.Notification is null)
        {
            return new DecisionEngineResult(
                DecisionCode.X00,
                nameof(UnlinkedNotificationDecisionRule),
                DecisionInternalFurtherDetail.E70
            );
        }

        return next(context);
    }
}
