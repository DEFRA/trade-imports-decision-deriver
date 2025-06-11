using Defra.TradeImportsDataApi.Domain.Ipaffs;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

public static class ImportPreNotificationExtensions
{
    public static DecisionImportPreNotification ToDecisionImportPreNotification(this ImportPreNotification notification)
    {
        var decisionNotification = new DecisionImportPreNotification
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
        };

        var commodities = notification.PartOne?.Commodities;

        var complementParameters = new Dictionary<int, ComplementParameterSets>();
        var complementRiskAssessments = new Dictionary<string, CommodityRiskResult>();

        if (commodities?.ComplementParameterSets != null)
        {
            foreach (var commoditiesCommodityComplement in commodities.ComplementParameterSets)
            {
                complementParameters[commoditiesCommodityComplement.ComplementId!.Value] =
                    commoditiesCommodityComplement;
            }
        }

        if (notification.RiskAssessment?.CommodityResults != null)
        {
            foreach (var commoditiesRa in notification.RiskAssessment.CommodityResults)
            {
                complementRiskAssessments[commoditiesRa.UniqueId!] = commoditiesRa;
            }
        }

        if (commodities?.CommodityComplements is null)
            return decisionNotification;

        decisionNotification.Commodities = commodities
            .CommodityComplements.Select(commodityComplement =>
            {
                var parameters = complementParameters[commodityComplement.ComplementId!.Value];

                if (
                    complementRiskAssessments.Count != 0
                    && parameters.UniqueComplementId is not null
                    && complementRiskAssessments.TryGetValue(parameters.UniqueComplementId, out var riskAssessmentValue)
                )
                {
                    return new DecisionCommodityComplement
                    {
                        HmiDecision = riskAssessmentValue.HmiDecision,
                        PhsiDecision = riskAssessmentValue.PhsiDecision,
                    };
                }

                return new DecisionCommodityComplement();
            })
            .ToArray();

        return decisionNotification;
    }
}
