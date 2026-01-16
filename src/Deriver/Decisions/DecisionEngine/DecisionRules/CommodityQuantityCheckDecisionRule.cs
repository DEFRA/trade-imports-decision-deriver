using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;
using Microsoft.Extensions.Options;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public sealed class CommodityQuantityCheckDecisionRule(IOptions<DecisionRulesOptions> options) : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        var result = next(context);

        if (!result.Code.IsReleaseOrHold())
        {
            return result;
        }

        var commodity = context.Commodity;
        var commodities = context
            .Notification.Commodities.Where(x =>
                x.CommodityCode != null && commodity.TaricCommodityCode?.StartsWith(x.CommodityCode) == true
            )
            .ToList();

        if (commodity.NetMass.HasValue)
        {
            CompareWeight(commodity, commodities, context.Logger);
        }
        else if (commodity.SupplementaryUnits.HasValue)
        {
            CompareQuantity(commodity, commodities, context.Logger);
        }

        return result;
    }

    private static void CompareQuantity(
        Commodity commodity,
        List<DecisionCommodityComplement> commodities,
        ILogger logger
    )
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

    private void CompareWeight(Commodity commodity, List<DecisionCommodityComplement> commodities, ILogger logger)
    {
        // Sum of nullable decimals returns nullable decimal; coalesce to 0 if null.
        var totalWeight = commodities.Sum(x => x.Weight) ?? 0m;

        var allowedWeight =
            commodity.NetMass.GetValueOrDefault() + options.Value.QuantityManagementCheckNetMassTolerance;

        if (totalWeight > allowedWeight)
        {
            logger.LogWarning(
                "Level 3 would have resulted in an X00 as IPAFFS NetWeight {NetWeight} is greater than allow in ClearanceRequest {CRNetWeight}",
                totalWeight,
                commodity.NetMass.HasValue
            );
        }

        if (totalWeight <= allowedWeight)
        {
            logger.LogInformation(
                "Level 3 would have succeeded as IPAFFS NetWeight {NetWeight} is less than allow in ClearanceRequest {CRNetWeight}",
                totalWeight,
                commodity.NetMass.HasValue
            );
        }
    }
}
