using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Configuration;

[ExcludeFromCodeCoverage]
public sealed class DecisionRulesOptions
{
    public const string SectionName = "DecisionRules";

    // Map of CHED type (e.g. "CHEDA", "CHEDP", ...) -> rules config for that CHED
    public Dictionary<string, DecisionRulesPerChedOptions> Cheds { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public decimal? QuantityManagementCheckNetMassTolerance { get; set; } = 0.04M;

    public RuleMode Level2Mode { get; set; } = RuleMode.DryRun;

    public RuleMode Level3Mode { get; set; } = RuleMode.DryRun;

    public CommodityQuantityCheckDecisionRuleOptions CommodityQuantityCheckDecisionRule { get; set; } = new();
}

[ExcludeFromCodeCoverage]
public sealed class DecisionRulesPerChedOptions
{
    // Class names of rules to disable for this CHED (e.g. "CommodityCodeValidationRule")
    public IEnumerable<string> DisabledRules { get; set; } = Array.Empty<string>();
}

public enum RuleMode
{
    DryRun,
    Live,
}

public sealed class CommodityQuantityCheckDecisionRuleOptions
{
    public CommodityQuantityCheckDecisionRuleScoringOptions Scoring { get; init; } = new();

    public List<CommodityQuantityCheckDecisionRuleComparisonEntry> ComparisonEntries { get; init; } = [];
}

public sealed class CommodityQuantityCheckDecisionRuleScoringOptions
{
    [Required]
    public int CommodityWeight { get; init; } = 100;

    [Required]
    public int CheckCodeWeight { get; init; } = 10;

    [Required]
    public int ChedTypeWeight { get; init; } = 1;
}

public enum QuantityComparisonType
{
    Weight,
    Quantity,
}

public sealed class CommodityQuantityCheckDecisionRuleComparisonEntry
{
    public string? ChedType { get; init; }
    public string? CheckCode { get; init; }
    public string? CommodityCode { get; init; }

    public QuantityComparisonType ComparisonType { get; init; }

    public bool UseFallback { get; init; } = true;

    public override string ToString()
    {
        return $"ChedType: {ChedType ?? "Any"}, CheckCode: {CheckCode ?? "Any"}, CommodityCode: {CommodityCode ?? "Any"}, ComparisonType: {ComparisonType}, UseFallback: {UseFallback}";
    }
}
