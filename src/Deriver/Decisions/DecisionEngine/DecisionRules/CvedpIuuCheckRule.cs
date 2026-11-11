namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public sealed class CvedpIuuCheckRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        if (!context.CheckCode.IsIuu())
        {
            return next(context);
        }

        var iuuOption = context.Notification.IuuOption;

        return iuuOption switch
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
        };
    }
}
