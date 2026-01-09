namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine.DecisionRules;

public sealed class CedDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionResolutionContext context, DecisionRuleDelegate next)
    {
        var notification = context.Notification;

        if (notification.HasAcceptableConsignmentDecision())
        {
            return notification.ConsignmentDecision switch
            {
                ConsignmentDecision.AcceptableForInternalMarket or ConsignmentDecision.AcceptableForNonInternalMarket =>
                    new DecisionEngineResult(DecisionCode.C03),
                _ => new DecisionEngineResult(DecisionCode.X00, DecisionInternalFurtherDetail.E96),
            };
        }

        if (notification.NotAcceptableAction is not null)
        {
            return notification.NotAcceptableAction switch
            {
                DecisionNotAcceptableAction.Destruction => new DecisionEngineResult(DecisionCode.N02),
                DecisionNotAcceptableAction.Redispatching => new DecisionEngineResult(DecisionCode.N04),
                DecisionNotAcceptableAction.Transformation => new DecisionEngineResult(DecisionCode.N03),
                DecisionNotAcceptableAction.Other => new DecisionEngineResult(DecisionCode.N07),
                _ => new DecisionEngineResult(DecisionCode.X00, DecisionInternalFurtherDetail.E97),
            };
        }

        if (notification.NotAcceptableReasons?.Length > 0)
        {
            return new DecisionEngineResult(DecisionCode.N04);
        }

        return new DecisionEngineResult(DecisionCode.X00, DecisionInternalFurtherDetail.E99);
    }
}
