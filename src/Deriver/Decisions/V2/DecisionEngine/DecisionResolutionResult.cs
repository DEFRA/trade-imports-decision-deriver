namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine;

public sealed record DecisionResolutionResult(DecisionCode Code, DecisionInternalFurtherDetail? FurtherDetail = null)
{
    public static DecisionResolutionResult UnknownDecision => new(DecisionCode.X00, DecisionInternalFurtherDetail.E99);

    public static DecisionResolutionResult WrongChedType => new(DecisionCode.X00, DecisionInternalFurtherDetail.E84);

    public static DecisionResolutionResult Unlinked => new(DecisionCode.X00, DecisionInternalFurtherDetail.E70);
}
