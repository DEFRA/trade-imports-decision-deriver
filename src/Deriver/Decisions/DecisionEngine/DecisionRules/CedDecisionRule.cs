namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public sealed class CedDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        var notification = context.Notification;

        if (notification.HasAcceptableConsignmentDecision())
        {
            return notification.ConsignmentDecision switch
            {
                ConsignmentDecision.AcceptableForInternalMarket or ConsignmentDecision.AcceptableForNonInternalMarket =>
                    new DecisionEngineResult(DecisionCode.C03, nameof(CedDecisionRule)),
                _ => new DecisionEngineResult(
                    DecisionCode.X00,
                    nameof(CedDecisionRule),
                    DecisionInternalFurtherDetail.E96
                ),
            };
        }

        if (notification.NotAcceptableAction is not null)
        {
            return notification.NotAcceptableAction switch
            {
                DecisionNotAcceptableAction.Destruction => new DecisionEngineResult(
                    DecisionCode.N02,
                    nameof(CedDecisionRule)
                ),
                DecisionNotAcceptableAction.Redispatching => new DecisionEngineResult(
                    DecisionCode.N04,
                    nameof(CedDecisionRule)
                ),
                DecisionNotAcceptableAction.Transformation => new DecisionEngineResult(
                    DecisionCode.N03,
                    nameof(CedDecisionRule)
                ),
                DecisionNotAcceptableAction.Other => new DecisionEngineResult(
                    DecisionCode.N07,
                    nameof(CedDecisionRule)
                ),
                _ => new DecisionEngineResult(
                    DecisionCode.X00,
                    nameof(CedDecisionRule),
                    DecisionInternalFurtherDetail.E97
                ),
            };
        }

        if (notification.NotAcceptableReasons?.Length > 0)
        {
            return new DecisionEngineResult(DecisionCode.N04, nameof(CedDecisionRule));
        }

        return new DecisionEngineResult(DecisionCode.X00, nameof(CedDecisionRule), DecisionInternalFurtherDetail.E99);
    }
}
