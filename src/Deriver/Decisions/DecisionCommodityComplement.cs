using System.ComponentModel;
using System.Text.Json.Serialization;
using Defra.TradeImportsDataApi.Domain.Ipaffs;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

public class DecisionCommodityComplement
{
    [JsonPropertyName("hmiDecision")]
    [Description("HMI decision required")]
    public CommodityRiskResultHmiDecision? HmiDecision { get; set; }

    [JsonPropertyName("phsiDecision")]
    [Description("PHSI decision required")]
    public CommodityRiskResultPhsiDecision? PhsiDecision { get; set; }
}

public static class ImportPreNotificationExtensions
{
    public static DecisionImportPreNotification ToDecisionImportPreNotification(this ImportPreNotification notification)
    {
        return new DecisionImportPreNotification()
        {
            Id = notification.ReferenceNumber!,
            UpdatedSource = notification.UpdatedSource,
            ImportNotificationType = notification.ImportNotificationType,
            Status = notification.Status,
            ConsignmentDecision = notification.PartTwo?.Decision?.ConsignmentDecision,
            NotAcceptableAction = notification.PartTwo?.Decision?.NotAcceptableAction,
            IuuCheckRequired = notification.PartTwo?.ControlAuthority?.IuuCheckRequired,
            IuuOption = notification.PartTwo?.ControlAuthority?.IuuOption,
            ConsignmentAcceptable = notification.PartTwo?.Decision?.ConsignmentAcceptable,
            NotAcceptableReasons = notification.PartTwo?.Decision?.NotAcceptableReasons,
            AutoClearedOn = notification.PartTwo?.AutoClearedOn,
            InspectionRequired = notification.PartTwo?.InspectionRequired,
            Commodities = notification
                .Commodities.Select(x => new DecisionCommodityComplement()
                {
                    HmiDecision = x.RiskAssesment?.HmiDecision,
                    PhsiDecision = x.RiskAssesment?.PhsiDecision,
                })
                .ToArray(),
        };
    }
}
