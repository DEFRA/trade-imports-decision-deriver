namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;

public enum DecisionResultMode
{
    Active = 1,
    Passive = 2,
}

public enum DecisionRuleLevel
{
    Level1 = 1,
    Level2 = 2,
    Level3 = 3,
}

public sealed record DecisionEngineResult(
    DecisionCode Code,
    string RuleName,
    DecisionInternalFurtherDetail? FurtherDetail = null,
    DecisionResultMode Mode = DecisionResultMode.Active,
    DecisionRuleLevel Level = DecisionRuleLevel.Level1
)
{
    public IList<DecisionEngineResult>? PassiveResults { get; private set; }

    public void AddResult(DecisionEngineResult passive)
    {
        ArgumentNullException.ThrowIfNull(passive);
        if (passive.Mode == DecisionResultMode.Active)
            throw new InvalidOperationException("Only passive results can be added to an active result.");

        PassiveResults ??= new List<DecisionEngineResult>();
        PassiveResults.Add(passive);
    }
}
