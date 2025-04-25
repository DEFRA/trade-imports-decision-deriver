using System.Text.Json.Serialization;
using Defra.TradeImportsDataApi.Domain.Ipaffs;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

public class DecisionCommodityComplement
{
    [JsonPropertyName("hmiDecision")]
    public CommodityRiskResultHmiDecision? HmiDecision { get; set; }

    [JsonPropertyName("phsiDecision")]
    public CommodityRiskResultPhsiDecision? PhsiDecision { get; set; }
}
