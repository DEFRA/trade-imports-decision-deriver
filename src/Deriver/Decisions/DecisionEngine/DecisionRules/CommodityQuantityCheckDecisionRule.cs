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

        if (!result.Code.IsReleaseOrHold() || context.Level2Succeeded == false)
        {
            return result;
        }

        var commodity = context.Commodity;
        var commodities = context
            .Notification.Commodities.Where(x =>
                x.CommodityCode != null && commodity.TaricCommodityCode?.StartsWith(x.CommodityCode) == true
            )
            .ToList();

        if (
            commodity.NetMass.HasValue
            && !WeightValid(context.ClearanceRequest.MovementReferenceNumber, commodity, commodities, context.Logger)
        )
        {
            var liveResult = ApplyLevel3Result(result, DecisionInternalFurtherDetail.E30);
            if (liveResult != null)
                return liveResult;
        }
        else if (
            commodity.SupplementaryUnits.HasValue
            && !QuantityValid(context.ClearanceRequest.MovementReferenceNumber, commodity, commodities, context.Logger)
        )
        {
            var liveResult = ApplyLevel3Result(result, DecisionInternalFurtherDetail.E31);
            if (liveResult != null)
                return liveResult;
        }

        return result;
    }

    private DecisionEngineResult? ApplyLevel3Result(
        DecisionEngineResult result,
        DecisionInternalFurtherDetail furtherDetail
    ) =>
        options.Value.Level3Mode switch
        {
            RuleMode.DryRun => AddPassiveResult(result, furtherDetail),
            RuleMode.Live => new DecisionEngineResult(
                DecisionCode.X00,
                nameof(CommodityQuantityCheckDecisionRule),
                furtherDetail,
                Level: DecisionRuleLevel.Level3
            ),
            _ => null,
        };

    private static DecisionEngineResult? AddPassiveResult(
        DecisionEngineResult result,
        DecisionInternalFurtherDetail furtherDetail
    )
    {
        result.AddResult(
            new DecisionEngineResult(
                DecisionCode.X00,
                nameof(CommodityQuantityCheckDecisionRule),
                furtherDetail,
                DecisionResultMode.Passive,
                DecisionRuleLevel.Level3
            )
        );
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool QuantityValid(
        string mrn,
        Commodity commodity,
        List<DecisionCommodityComplement> commodities,
        ILogger logger
    )
    {
        var totalQuantity = commodities.Sum(x => x.Quantity);
        return totalQuantity <= commodity.SupplementaryUnits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool WeightValid(
        string mrn,
        Commodity commodity,
        List<DecisionCommodityComplement> commodities,
        ILogger logger
    )
    {
        var totalWeight = commodities.Sum(x => x.Weight) ?? 0m;
        var allowedWeight =
            commodity.NetMass.GetValueOrDefault() + options.Value.QuantityManagementCheckNetMassTolerance;
        return totalWeight <= allowedWeight;
    }
}
