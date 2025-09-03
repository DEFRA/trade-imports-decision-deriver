using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
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
            NotAcceptableReasons = notification.PartTwo?.Decision?.NotAcceptableReasons,
            InspectionRequired = notification.PartTwo?.InspectionRequired,
            HasPartTwo = notification.PartTwo != null,
        };

        var commodityChecks = new List<DecisionCommodityCheck>();
        if (notification.PartTwo?.CommodityChecks != null)
        {
            foreach (var commodityCheck in notification.PartTwo.CommodityChecks.Where(x => x.Checks != null))
            {
                var checks = new List<DecisionCommodityCheck.Check>();
                checks.AddRange(
                    commodityCheck.Checks!.Select(check => new DecisionCommodityCheck.Check()
                    {
                        Status = check.Status ?? string.Empty,
                        Type = check.Type,
                    })
                );
                commodityChecks.Add(new DecisionCommodityCheck() { Checks = checks.ToArray() });
            }
        }

        decisionNotification.CommodityChecks = commodityChecks.SelectMany(x => x.Checks).ToArray();

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
                if (!complementParameters.TryGetValue(commodityComplement.ComplementId!.Value, out var parameters))
                {
                    return new DecisionCommodityComplement();
                }

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
