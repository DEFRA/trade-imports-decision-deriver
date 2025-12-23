namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine.DecisionRules;

public sealed class CvedpIuuCheckRule : IDecisionRule
{
    public DecisionResolutionResult Execute(DecisionResolutionContext context, DecisionRuleDelegate next)
    {
        if (!context.CheckCode.IsIuu())
        {
            return next(context);
        }

        var notification = context.Notification;
        return (notification.IuuCheckRequired == true) switch
        {
            true => notification.IuuOption switch
            {
                ControlAuthorityIuuOption.IUUOK => new DecisionResolutionResult(DecisionCode.C07),
                ControlAuthorityIuuOption.IUUNotCompliant => new DecisionResolutionResult(DecisionCode.X00),
                ControlAuthorityIuuOption.IUUNA => new DecisionResolutionResult(DecisionCode.C08),
                _ => new DecisionResolutionResult(DecisionCode.H02, DecisionInternalFurtherDetail.E93),
            },
            false => new DecisionResolutionResult(DecisionCode.H02, DecisionInternalFurtherDetail.E94),
        };
    }
}
