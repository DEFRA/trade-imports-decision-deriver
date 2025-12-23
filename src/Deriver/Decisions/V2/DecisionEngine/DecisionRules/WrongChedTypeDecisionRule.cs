namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine.DecisionRules;

public sealed class WrongChedTypeDecisionRule : IDecisionRule
{
    public DecisionResolutionResult Execute(DecisionResolutionContext context, DecisionRuleDelegate next)
    {
        if (context.Notification.ImportNotificationType != context.CheckCode.GetImportNotificationType())
        {
            return DecisionResolutionResult.WrongChedType;
        }

        return next(context);
    }
}
