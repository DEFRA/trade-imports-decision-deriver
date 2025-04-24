using System.Text.Json.Serialization;
using Defra.TradeImportsDataApi.Domain.Ipaffs;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

public class DecisionImportPreNotification
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("updatedSource")]
    public DateTime? UpdatedSource { get; set; }

    [JsonPropertyName("consignmentAcceptable")]
    public bool? ConsignmentAcceptable { get; set; }

    [JsonPropertyName("notAcceptableAction")]
    public DecisionNotAcceptableAction? NotAcceptableAction { get; set; }

    [JsonPropertyName("notAcceptableReasons")]
    public string[]? NotAcceptableReasons { get; set; }

    [JsonPropertyName("consignmentDecision")]
    public ConsignmentDecision? ConsignmentDecision { get; set; }

    [JsonPropertyName("iuuCheckRequired")]
    public bool? IuuCheckRequired { get; set; }

    [JsonPropertyName("iuuOption")]
    public ControlAuthorityIuuOption? IuuOption { get; set; }

    [JsonPropertyName("inspectionRequired")]
    public InspectionRequired? InspectionRequired { get; set; }

    [JsonPropertyName("autoClearedOn")]
    public DateTime? AutoClearedOn { get; set; }

    [JsonPropertyName("importNotificationType")]
    public ImportNotificationType? ImportNotificationType { get; set; }

    [JsonPropertyName("status")]
    public ImportNotificationStatus? Status { get; set; }

    [JsonPropertyName("commodities")]
    public DecisionCommodityComplement[] Commodities { get; set; } = [];
}
