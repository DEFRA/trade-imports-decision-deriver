using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

public static class DecisionCodeExtensions
{
    public static bool IsReleaseOrHold(this DecisionCode decisionCode)
    {
        return decisionCode switch
        {
            DecisionCode.C02 or DecisionCode.C03 or DecisionCode.C05 or DecisionCode.C06 or DecisionCode.C07
                or DecisionCode.C08 or DecisionCode.H01 or DecisionCode.H02 => true,
            _ => false
        };
    }
}