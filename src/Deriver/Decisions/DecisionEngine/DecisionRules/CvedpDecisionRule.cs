using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public sealed class CvedpDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        var notification = context.Notification;

        if (notification.StatusIsSubmittedOrInProgress())
        {
            return new DecisionEngineResult(
                DecisionCode.H01,
                nameof(CvedpDecisionRule)
            );
        }

        if (notification.HasAcceptableConsignmentDecision())
        {
            return notification.ConsignmentDecision switch
            {
                ConsignmentDecision.AcceptableForTranshipment
                or ConsignmentDecision.AcceptableForTransit
                or ConsignmentDecision.AcceptableForSpecificWarehouse => new DecisionEngineResult(
                    DecisionCode.E03,
                    nameof(CvedpDecisionRule)
                ),
                ConsignmentDecision.AcceptableForInternalMarket => new DecisionEngineResult(
                    DecisionCode.C03,
                    nameof(CvedpDecisionRule)
                ),
                ConsignmentDecision.AcceptableIfChanneled => new DecisionEngineResult(
                    DecisionCode.C06,
                    nameof(CvedpDecisionRule)
                ),
                _ => new DecisionEngineResult(
                    DecisionCode.X00,
                    nameof(CvedpDecisionRule),
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
                    nameof(CvedpDecisionRule)
                ),
                DecisionNotAcceptableAction.Reexport => new DecisionEngineResult(
                    DecisionCode.N04,
                    nameof(CvedpDecisionRule)
                ),
                DecisionNotAcceptableAction.Transformation => new DecisionEngineResult(
                    DecisionCode.N03,
                    nameof(CvedpDecisionRule)
                ),
                DecisionNotAcceptableAction.Other => new DecisionEngineResult(
                    DecisionCode.N07,
                    nameof(CvedpDecisionRule)
                ),
                _ => new DecisionEngineResult(
                    DecisionCode.X00,
                    nameof(CvedpDecisionRule),
                    DecisionInternalFurtherDetail.E97
                ),
            };
        }

        if (notification.NotAcceptableReasons?.Length > 0)
        {
            return new DecisionEngineResult(DecisionCode.N04, nameof(CvedpDecisionRule));
        }

        return new DecisionEngineResult(DecisionCode.X00, nameof(CvedpDecisionRule), DecisionInternalFurtherDetail.E99);
    }
}
