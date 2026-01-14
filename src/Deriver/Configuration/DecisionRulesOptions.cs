namespace Defra.TradeImportsDecisionDeriver.Deriver.Configuration;

public sealed class DecisionRulesOptions
{
    // Map of CHED type (e.g. "CHEDA", "CHEDP", ...) -> rules config for that CHED
    public Dictionary<string, DecisionRulesPerChedOptions> Cheds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class DecisionRulesPerChedOptions
{
    // Class names of rules to disable for this CHED (e.g. "CommodityCodeValidationRule")
    public IEnumerable<string> DisabledRules { get; set; } = Array.Empty<string>();
}
