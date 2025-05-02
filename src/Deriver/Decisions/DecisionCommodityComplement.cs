using System.Text.Json.Serialization;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

public class DecisionCommodityComplement
{
    [JsonPropertyName("hmiDecision")]
    public string? HmiDecision { get; set; }

    [JsonPropertyName("phsiDecision")]
    public string? PhsiDecision { get; set; }
}
