using System.Text.Json.Serialization;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

public class DecisionImportPreNotification
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("updatedSource")]
    public DateTime? UpdatedSource { get; set; }

    [JsonPropertyName("notAcceptableAction")]
    public string? NotAcceptableAction { get; set; }

    [JsonPropertyName("notAcceptableReasons")]
    public string[]? NotAcceptableReasons { get; set; }

    [JsonPropertyName("consignmentDecision")]
    public string? ConsignmentDecision { get; set; }

    [JsonPropertyName("iuuCheckRequired")]
    public bool? IuuCheckRequired { get; set; }

    [JsonPropertyName("iuuOption")]
    public string? IuuOption { get; set; }

    [JsonPropertyName("inspectionRequired")]
    public string? InspectionRequired { get; set; }

    [JsonPropertyName("importNotificationType")]
    public string? ImportNotificationType { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("commodities")]
    public DecisionCommodityComplement[] Commodities { get; set; } = [];

    [JsonPropertyName("commodityChecks")]
    public DecisionCommodityCheck.Check[] CommodityChecks { get; set; } = [];

    public bool HasAcceptableConsignmentDecision()
    {
        return ConsignmentDecision is not null
            && ConsignmentDecision
                != Defra.TradeImportsDecisionDeriver.Deriver.Decisions.ConsignmentDecision.NonAcceptable;
    }

    public string GetVersion()
    {
        return $"{Id}_{Status}_{UpdatedSource:o}";
    }
}
