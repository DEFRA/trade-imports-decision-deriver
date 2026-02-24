namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public sealed class CvedaDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        var notification = context.Notification;

        if (notification.HasAcceptableConsignmentDecision())
        {
            return notification.ConsignmentDecision switch
            {
                ConsignmentDecision.AcceptableForTranshipment or ConsignmentDecision.AcceptableForTransit =>
                    DecisionEngineResult.E03,
                ConsignmentDecision.AcceptableForInternalMarket => DecisionEngineResult.C03,
                ConsignmentDecision.AcceptableForTemporaryImport => DecisionEngineResult.C05,
                ConsignmentDecision.HorseReEntry => DecisionEngineResult.C06,
                _ => DecisionEngineResult.X00E96,
            };
        }

        if (notification.NotAcceptableAction is not null)
        {
            return notification.NotAcceptableAction switch
            {
                DecisionNotAcceptableAction.Euthanasia or DecisionNotAcceptableAction.Slaughter =>
                    DecisionEngineResult.N02,
                DecisionNotAcceptableAction.Reexport => DecisionEngineResult.N04,
                _ => DecisionEngineResult.X00E97,
            };
        }

        if (notification.NotAcceptableReasons?.Length > 0)
        {
            return DecisionEngineResult.N04;
        }

        return next(context);
    }
}
