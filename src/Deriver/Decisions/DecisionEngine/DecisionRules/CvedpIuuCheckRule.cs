namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public sealed class CvedpIuuCheckRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
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
                ControlAuthorityIuuOption.IUUOK => DecisionEngineResult.C07,
                ControlAuthorityIuuOption.IUUNotCompliant => DecisionEngineResult.X00,
                ControlAuthorityIuuOption.IUUNA => DecisionEngineResult.C08,
                _ => DecisionEngineResult.H02E93,
            },
            false => DecisionEngineResult.H02E94,
        };
    }
}
