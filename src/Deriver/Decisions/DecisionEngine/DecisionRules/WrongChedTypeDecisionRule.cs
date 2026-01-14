using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public sealed class WrongChedTypeDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        if (context.Notification.ImportNotificationType != context.CheckCode.GetImportNotificationType())
        {
            return DecisionEngineResult.WrongChedType;
        }

        return next(context);
    }
}
