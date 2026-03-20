namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public sealed class WrongChedTypeDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        if (context.Notification.ImportNotificationType != context.CheckCode.GetImportNotificationType())
        {
            return new DecisionEngineResult(
                DecisionCode.X00,
                nameof(WrongChedTypeDecisionRule),
                DecisionInternalFurtherDetail.E84
            );
        }

        return next(context);
    }
}
