using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;

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
                    new DecisionEngineResult(DecisionCode.E03),
                ConsignmentDecision.AcceptableForInternalMarket => new DecisionEngineResult(DecisionCode.C03),
                ConsignmentDecision.AcceptableForTemporaryImport => new DecisionEngineResult(DecisionCode.C05),
                ConsignmentDecision.HorseReEntry => new DecisionEngineResult(DecisionCode.C06),
                _ => new DecisionEngineResult(DecisionCode.X00, DecisionInternalFurtherDetail.E96),
            };
        }

        if (notification.NotAcceptableAction is not null)
        {
            return notification.NotAcceptableAction switch
            {
                DecisionNotAcceptableAction.Euthanasia or DecisionNotAcceptableAction.Slaughter =>
                    new DecisionEngineResult(DecisionCode.N02),
                DecisionNotAcceptableAction.Reexport => new DecisionEngineResult(DecisionCode.N04),
                _ => new DecisionEngineResult(DecisionCode.X00, DecisionInternalFurtherDetail.E97),
            };
        }

        if (notification.NotAcceptableReasons?.Length > 0)
        {
            return new DecisionEngineResult(DecisionCode.N04);
        }

        return next(context);
    }
}
