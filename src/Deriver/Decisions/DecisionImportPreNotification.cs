using System.Text.Json.Serialization;
using Defra.TradeImportsDataApi.Domain.Ipaffs;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

public class DecisionImportPreNotification
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("consignmentAcceptable")]
    [System.ComponentModel.Description("Is consignment acceptable or not")]
    public bool? ConsignmentAcceptable { get; set; }

    [JsonPropertyName("notAcceptableAction")]
    public DecisionNotAcceptableAction? NotAcceptableAction { get; set; }

    [JsonPropertyName("notAcceptableReasons")]
    [System.ComponentModel.Description("If the consignment was not accepted what was the reason")]
    public string[]? NotAcceptableReasons { get; set; }

    [JsonPropertyName("consignmentDecision")]
    public ConsignmentDecision? ConsignmentDecision { get; set; }

    [JsonPropertyName("iuuCheckRequired")]
    [System.ComponentModel.Description("Was Illegal, Unreported and Unregulated (IUU) check required")]
    public bool? IuuCheckRequired { get; set; }

    [JsonPropertyName("iuuOption")]
    [System.ComponentModel.Description("Result of Illegal, Unreported and Unregulated (IUU) check")]
    public ControlAuthorityIuuOption? IuuOption { get; set; }

    [JsonPropertyName("inspectionRequired")]
    [System.ComponentModel.Description("Inspection required")]
    public InspectionRequired? InspectionRequired { get; set; }

    [JsonPropertyName("autoClearedOn")]
    [System.ComponentModel.Description("Date of autoclearance")]
    public DateTime? AutoClearedOn { get; set; }

    [JsonPropertyName("importNotificationType")]
    public Defra.TradeImportsDataApi.Domain.Ipaffs.ImportNotificationType? ImportNotificationType { get; set; }

    [JsonPropertyName("status")]
    public ImportNotificationStatus? Status { get; set; }

    [JsonPropertyName("commodities")]
    public DecisionCommodityComplement[] Commodities { get; set; } = [];
}
