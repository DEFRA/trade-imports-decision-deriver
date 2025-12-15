using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

public class CommodityWeightOrQualityDecisionFinder(
    IDecisionFinder innerDecisionFinder,
    ILogger<CommodityWeightOrQualityDecisionFinder> logger
) : IDecisionFinder
{
    public string ChedType => innerDecisionFinder.ChedType;

    public bool CanFindDecision(DecisionImportPreNotification notification, CheckCode? checkCode, string? documentCode)
    {
        return innerDecisionFinder.CanFindDecision(notification, checkCode, documentCode);
    }

    public DecisionFinderResult FindDecision(
        DecisionImportPreNotification notification,
        Commodity commodity,
        CheckCode? checkCode
    )
    {
        var result = innerDecisionFinder.FindDecision(notification, commodity, checkCode);

        if (!result.DecisionCode.IsReleaseOrHold())
            return result;

        var commodities = notification
            .Commodities.Where(x =>
                x.CommodityCode != null && commodity.TaricCommodityCode?.StartsWith(x.CommodityCode) == true
            )
            .ToList();

        if (commodity.NetMass.HasValue)
        {
            CompareWeight(commodity, commodities);
        }
        else if (commodity.SupplementaryUnits.HasValue)
        {
            CompareQuantity(commodity, commodities);
        }

        return result;
    }

    private void CompareQuantity(Commodity commodity, List<DecisionCommodityComplement> commodities)
    {
        var totalQuantity = commodities.Sum(x => x.Quantity);
        if (totalQuantity > commodity.SupplementaryUnits)
        {
            logger.LogWarning(
                "Level 3 would have resulted in an X00 as IPAFFS NetQuantity {NetQuantity} is greater than allow in ClearanceRequest {CRNetQuantity}",
                totalQuantity,
                commodity.NetMass.HasValue
            );
        }

        if (totalQuantity < commodity.SupplementaryUnits)
        {
            logger.LogInformation(
                "Level 3 would have succeeded as IPAFFS NetQuantity {NetQuantity} is less than allow in ClearanceRequest {CRNetWeight}",
                totalQuantity,
                commodity.NetMass.HasValue
            );
        }
    }

    private void CompareWeight(Commodity commodity, List<DecisionCommodityComplement> commodities)
    {
        var totalWeight = commodities.Sum(x => x.Weight);
        if (totalWeight > commodity.NetMass)
        {
            logger.LogWarning(
                "Level 3 would have resulted in an X00 as IPAFFS NetWeight {NetWeight} is greater than allow in ClearanceRequest {CRNetWeight}",
                totalWeight,
                commodity.NetMass.HasValue
            );
        }

        if (totalWeight < commodity.NetMass)
        {
            logger.LogInformation(
                "Level 3 would have succeeded as IPAFFS NetWeight {NetWeight} is less than allow in ClearanceRequest {CRNetWeight}",
                totalWeight,
                commodity.NetMass.HasValue
            );
        }
    }
}
