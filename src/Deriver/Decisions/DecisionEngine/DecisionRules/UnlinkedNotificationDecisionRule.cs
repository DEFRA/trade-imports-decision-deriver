using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public sealed class UnlinkedNotificationDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        if (context.Notification is null)
        {
            return DecisionEngineResult.Unlinked;
        }

        return next(context);
    }
}
