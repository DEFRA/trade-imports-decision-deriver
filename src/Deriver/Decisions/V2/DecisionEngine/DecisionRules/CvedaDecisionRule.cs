namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine.DecisionRules;

public sealed class CvedaDecisionRule : IDecisionRule
{
    public DecisionResolutionResult Execute(DecisionResolutionContext context, DecisionRuleDelegate next)
    {
        var notification = context.Notification;

        if (notification.HasAcceptableConsignmentDecision())
        {
            return notification.ConsignmentDecision switch
            {
                ConsignmentDecision.AcceptableForTranshipment or ConsignmentDecision.AcceptableForTransit =>
                    new DecisionResolutionResult(DecisionCode.E03),
                ConsignmentDecision.AcceptableForInternalMarket => new DecisionResolutionResult(DecisionCode.C03),
                ConsignmentDecision.AcceptableForTemporaryImport => new DecisionResolutionResult(DecisionCode.C05),
                ConsignmentDecision.HorseReEntry => new DecisionResolutionResult(DecisionCode.C06),
                _ => new DecisionResolutionResult(DecisionCode.X00, DecisionInternalFurtherDetail.E96),
            };
        }

        if (notification.NotAcceptableAction is not null)
        {
            return notification.NotAcceptableAction switch
            {
                DecisionNotAcceptableAction.Euthanasia or DecisionNotAcceptableAction.Slaughter =>
                    new DecisionResolutionResult(DecisionCode.N02),
                DecisionNotAcceptableAction.Reexport => new DecisionResolutionResult(DecisionCode.N04),
                _ => new DecisionResolutionResult(DecisionCode.X00, DecisionInternalFurtherDetail.E97),
            };
        }

        if (notification.NotAcceptableReasons?.Length > 0)
        {
            return new DecisionResolutionResult(DecisionCode.N04);
        }

        return next(context);
    }
}
