namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine.DecisionRules;

public sealed class WrongChedTypeDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionResolutionContext context, DecisionRuleDelegate next)
    {
        if (context.Notification.ImportNotificationType != context.CheckCode.GetImportNotificationType())
        {
            return DecisionEngineResult.WrongChedType;
        }

        return next(context);
    }
}
