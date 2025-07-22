using System.Text.Json.Serialization;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

public class DecisionCommodityComplement
{
    [JsonPropertyName("hmiDecision")]
    public string? HmiDecision { get; set; }

    [JsonPropertyName("phsiDecision")]
    public string? PhsiDecision { get; set; }
}

public class DecisionCommodityCheck
{
    [JsonPropertyName("checks")]
    public Check[] Checks { get; set; } = [];

    public class Check
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("status")]
        public required string Status { get; set; }
    }
}
