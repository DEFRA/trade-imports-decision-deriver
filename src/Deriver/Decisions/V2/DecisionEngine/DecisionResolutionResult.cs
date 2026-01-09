namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine;

public sealed record DecisionEngineResult(DecisionCode Code, DecisionInternalFurtherDetail? FurtherDetail = null)
{
    public static DecisionEngineResult UnknownDecision => new(DecisionCode.X00, DecisionInternalFurtherDetail.E99);

    public static DecisionEngineResult WrongChedType => new(DecisionCode.X00, DecisionInternalFurtherDetail.E84);

    public static DecisionEngineResult Unlinked => new(DecisionCode.X00, DecisionInternalFurtherDetail.E70);
}
