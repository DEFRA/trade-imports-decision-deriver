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
                ControlAuthorityIuuOption.IUUOK => new DecisionEngineResult(
                    DecisionCode.C07,
                    nameof(CvedpIuuCheckRule)
                ),
                ControlAuthorityIuuOption.IUUNotCompliant => new DecisionEngineResult(
                    DecisionCode.X00,
                    nameof(CvedpIuuCheckRule)
                ),
                ControlAuthorityIuuOption.IUUNA => new DecisionEngineResult(
                    DecisionCode.C08,
                    nameof(CvedpIuuCheckRule)
                ),
                _ => new DecisionEngineResult(
                    DecisionCode.H02,
                    nameof(CvedpIuuCheckRule),
                    DecisionInternalFurtherDetail.E93
                ),
            },
            false => new DecisionEngineResult(
                DecisionCode.H02,
                nameof(CvedpIuuCheckRule),
                DecisionInternalFurtherDetail.E94
            ),
        };
    }
}
