using static Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionCommodityCheck;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine.DecisionRules;

public sealed class UnlinkedNotificationDecisionRule : IDecisionRule
{
    public DecisionResolutionResult Execute(DecisionResolutionContext context, DecisionRuleDelegate next)
    {
        if (context.Notification is null)
        {
            return DecisionResolutionResult.Unlinked;
        }

        return next(context);
    }
}
