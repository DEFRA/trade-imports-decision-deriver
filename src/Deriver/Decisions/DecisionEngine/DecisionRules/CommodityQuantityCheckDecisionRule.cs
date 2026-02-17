using System.Runtime.CompilerServices;
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

        if (!result.Code.IsReleaseOrHold() || !context.Level2Succeeded == false)
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
            CompareWeight(context.ClearanceRequest.MovementReferenceNumber, commodity, commodities, context.Logger);
        }
        else if (commodity.SupplementaryUnits.HasValue)
        {
            CompareQuantity(context.ClearanceRequest.MovementReferenceNumber, commodity, commodities, context.Logger);
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CompareQuantity(
        string mrn,
        Commodity commodity,
        List<DecisionCommodityComplement> commodities,
        ILogger logger
    )
    {
        var ipaffsCount = commodities.Sum(x => x.Quantity);
        if (commodity.SupplementaryUnits > ipaffsCount)
        {
            logger.LogWarning(
                "{MRN} - Level 3 would have resulted in a No Match as Item {ItemNumber} on Clearance Request has a quantity of {CRNetQuantity}, but associated item(s) on IPAFFS have a quanitity of {NetQuantity}",
                mrn,
                commodity.ItemNumber,
                commodity.SupplementaryUnits,
                ipaffsCount
            );
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CompareWeight(
        string mrn,
        Commodity commodity,
        List<DecisionCommodityComplement> commodities,
        ILogger logger
    )
    {
        // Sum of nullable decimals returns nullable decimal; coalesce to 0 if null.
        var ipaffsWeight = commodities.Sum(x => x.Weight) ?? 0m;

        var crWeight = commodity.NetMass.GetValueOrDefault() + options.Value.QuantityManagementCheckNetMassTolerance;

        if (crWeight > ipaffsWeight)
        {
            logger.LogWarning(
                "{MRN} - Level 3 would have resulted in a No Match as Item {ItemNumber} on Clearance Request has a weight of {CRNetWeight}, but associated item(s) on IPAFFS have a weight of {NetWeight}",
                mrn,
                commodity.ItemNumber,
                crWeight,
                ipaffsWeight
            );
        }
    }
}
