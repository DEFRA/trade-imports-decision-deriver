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
                    DecisionEngineResult.C03,
                _ => DecisionEngineResult.X00E96,
            };
        }

        if (notification.NotAcceptableAction is not null)
        {
            return notification.NotAcceptableAction switch
            {
                DecisionNotAcceptableAction.Destruction => DecisionEngineResult.N02,
                DecisionNotAcceptableAction.Redispatching => DecisionEngineResult.N04,
                DecisionNotAcceptableAction.Transformation => DecisionEngineResult.N03,
                DecisionNotAcceptableAction.Other => DecisionEngineResult.N07,
                _ => DecisionEngineResult.X00E97,
            };
        }

        if (notification.NotAcceptableReasons?.Length > 0)
        {
            return DecisionEngineResult.N04;
        }

        return DecisionEngineResult.X00E99;
    }
}
