using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public sealed class CvedaDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        var notification = context.Notification;

        if (notification.StatusIsSubmittedOrInProgress())
        {
            return new DecisionEngineResult(
                DecisionCode.H01,
                nameof(CvedaDecisionRule)
            );
        }

        if (notification.HasAcceptableConsignmentDecision())
        {
            return notification.ConsignmentDecision switch
            {
                ConsignmentDecision.AcceptableForTranshipment or ConsignmentDecision.AcceptableForTransit =>
                    new DecisionEngineResult(DecisionCode.E03, nameof(CvedaDecisionRule)),
                ConsignmentDecision.AcceptableForInternalMarket => new DecisionEngineResult(
                    DecisionCode.C03,
                    nameof(CvedaDecisionRule)
                ),
                ConsignmentDecision.AcceptableForTemporaryImport => new DecisionEngineResult(
                    DecisionCode.C05,
                    nameof(CvedaDecisionRule)
                ),
                ConsignmentDecision.HorseReEntry => new DecisionEngineResult(
                    DecisionCode.C06,
                    nameof(CvedaDecisionRule)
                ),
                _ => new DecisionEngineResult(
                    DecisionCode.X00,
                    nameof(CvedaDecisionRule),
                    DecisionInternalFurtherDetail.E96
                ),
            };
        }

        if (notification.NotAcceptableAction is not null)
        {
            return notification.NotAcceptableAction switch
            {
                DecisionNotAcceptableAction.Euthanasia or DecisionNotAcceptableAction.Slaughter =>
                    new DecisionEngineResult(DecisionCode.N02, nameof(CvedaDecisionRule)),
                DecisionNotAcceptableAction.Reexport => new DecisionEngineResult(
                    DecisionCode.N04,
                    nameof(CvedaDecisionRule)
                ),
                _ => new DecisionEngineResult(
                    DecisionCode.X00,
                    nameof(CvedaDecisionRule),
                    DecisionInternalFurtherDetail.E97
                ),
            };
        }

        if (notification.NotAcceptableReasons?.Length > 0)
        {
            return new DecisionEngineResult(DecisionCode.N04, nameof(CvedaDecisionRule));
        }

        return next(context);
    }
}
