using System.Diagnostics.CodeAnalysis;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Configuration;

[ExcludeFromCodeCoverage]
public sealed class DecisionRulesOptions
{
    public const string SectionName = "DecisionRules";

    // Map of CHED type (e.g. "CHEDA", "CHEDP", ...) -> rules config for that CHED
    public Dictionary<string, DecisionRulesPerChedOptions> Cheds { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public decimal? QuantityManagementCheckNetMassTolerance { get; set; } = 0.04M;
}

[ExcludeFromCodeCoverage]
public sealed class DecisionRulesPerChedOptions
{
    // Class names of rules to disable for this CHED (e.g. "CommodityCodeValidationRule")
    public IEnumerable<string> DisabledRules { get; set; } = Array.Empty<string>();
}
