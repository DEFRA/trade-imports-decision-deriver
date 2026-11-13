using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsDataApi.Domain.Ipaffs.Constants;

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

        var complementParameters = new Dictionary<string, ComplementParameterSets>();
        var complementRiskAssessments = new Dictionary<string, CommodityRiskResult>();

        if (commodities?.ComplementParameterSets != null)
        {
            foreach (var commoditiesCommodityComplement in commodities.ComplementParameterSets)
            {
                complementParameters[
                    $"{commoditiesCommodityComplement.ComplementId}_{commoditiesCommodityComplement.SpeciesId}"
                ] = commoditiesCommodityComplement;
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
                CreateDecisionCommodityComplement(
                    commodityComplement,
                    complementParameters,
                    complementRiskAssessments,
                    notification.ImportNotificationType
                )
            )
            .ToArray();

        return decisionNotification;
    }

    private static DecisionCommodityComplement CreateDecisionCommodityComplement(
        CommodityComplements commodityComplement,
        Dictionary<string, ComplementParameterSets> complementParameters,
        Dictionary<string, CommodityRiskResult> complementRiskAssessments,
        string? importNotificationType
    )
    {
        var decisionCommodityComplement = new DecisionCommodityComplement
        {
            Id = commodityComplement.ComplementId,
            CommodityCode = commodityComplement.CommodityId,
        };

        if (
            !complementParameters.TryGetValue(
                $"{commodityComplement.ComplementId}_{commodityComplement.SpeciesId}",
                out var parameters
            )
        )
        {
            return decisionCommodityComplement;
        }

        if (parameters.KeyDataPairs != null)
        {
            decisionCommodityComplement.Weight = parameters
                .KeyDataPairs.Where(x => x.Key == "netweight")
                .Select(x => decimal.TryParse(x.Data, out var v) ? v : (decimal?)null)
                .FirstOrDefault();

            var quantityKey =
                importNotificationType == ImportNotificationType.Cveda ? "number_animal" : "number_package";

            decisionCommodityComplement.Quantity = parameters
                .KeyDataPairs.Where(x => x.Key == quantityKey)
                .Select(x => int.TryParse(x.Data, out var v) ? v : (int?)null)
                .FirstOrDefault();
        }

        if (
            complementRiskAssessments.Count != 0
            && parameters.UniqueComplementId is not null
            && complementRiskAssessments.TryGetValue(parameters.UniqueComplementId, out var riskAssessmentValue)
        )
        {
            decisionCommodityComplement.HmiDecision = riskAssessmentValue.HmiDecision;
            decisionCommodityComplement.PhsiDecision = riskAssessmentValue.PhsiDecision;
        }

        return decisionCommodityComplement;
    }
}
