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

        var mrnCommodity = context.Commodity;
        var chedCommodities = context
            .Notification.Commodities.Where(x =>
                x.CommodityCode != null && mrnCommodity.TaricCommodityCode?.StartsWith(x.CommodityCode) == true
            )
            .ToList();

        var commodities =
            context.ClearanceRequest.CustomsDeclaration.ClearanceRequest?.Commodities ?? Enumerable.Empty<Commodity>();

        var mrnCommodities = commodities
            .Where(mrn => chedCommodities.Any(ched => mrn.TaricCommodityCode?.StartsWith(ched.CommodityCode!) == true))
            .Where(mrn =>
                mrn.Documents != null
                && mrn.Documents.Any(d =>
                    d.GetDocumentReferenceIdentifier() == context.ImportDocument?.GetDocumentReferenceIdentifier()
                )
            )
            .ToList();

        if (mrnCommodity.NetMass.HasValue)
        {
            if (
                !WeightValid(
                    context.ClearanceRequest.MovementReferenceNumber,
                    mrnCommodity,
                    mrnCommodities,
                    chedCommodities,
                    context.Logger
                )
            )
            {
                var liveResult = ApplyLevel3Result(result, DecisionInternalFurtherDetail.E30);
                if (liveResult != null)
                    return liveResult;
            }
        }
        else if (
            mrnCommodity.SupplementaryUnits.HasValue
            && !QuantityValid(
                context.ClearanceRequest.MovementReferenceNumber,
                mrnCommodity,
                mrnCommodities,
                chedCommodities,
                context.Logger
            )
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
        List<Commodity> mrnCommodities,
        List<DecisionCommodityComplement> commodities,
        ILogger logger
    )
    {
        var chedWeight = commodities.Sum(x => x.Quantity);

        var mrnWeight = mrnCommodities.Sum(x => x.SupplementaryUnits);
        var difference = chedWeight - mrnWeight;

        if (difference < 0)
        {
            logger.LogWarning(
                "{MRN} would not match at Level 3 due to a discrepancy on the quantity values. The item with the discrepancy is {TaricCommodityCode} & {GoodsDescription}, the weight on the MRN is {ItemQuantity}, the weight on the CHED is {ChedQuantity}, the difference is {Difference}",
                mrn,
                commodity.TaricCommodityCode,
                commodity.GoodsDescription,
                mrnWeight,
                chedWeight,
                difference
            );

            var mrnWeights = string.Join(
                ", ",
                mrnCommodities.Select(x =>
                    $"Item: {x.ItemNumber} - Code: {x.TaricCommodityCode} - Quantity: {x.SupplementaryUnits}"
                )
            );

            var chedWeights = string.Join(
                ", ",
                commodities.Select(x => $"Code: {x.CommodityCode} - Quantity: {x.Quantity}")
            );

            logger.LogInformation("Weights used. MRN: [{MrnQuantities}] CHED: [{ChedQuantities}]", mrnWeights, chedWeights);
        }

        return difference >= 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool WeightValid(
        string mrn,
        Commodity commodity,
        List<Commodity> mrnCommodities,
        List<DecisionCommodityComplement> commodities,
        ILogger logger
    )
    {
        var totalWeight = commodities.Sum(x => x.Weight) ?? 0m;
        var mrnWeight = mrnCommodities.Sum(x => x.NetMass);
        var chedWeight = totalWeight + options.Value.QuantityManagementCheckNetMassTolerance;

        var difference = chedWeight - mrnWeight;

        if (difference < 0)
        {
            logger.LogWarning(
                "{MRN} would not match at Level 3 due to a discrepancy on the weight values. The item with the discrepancy is {TaricCommodityCode} & {GoodsDescription}, the weight on the MRN is {ItemNetMass}, the weight on the CHED is {ChedWeight}, the difference is {Difference}, and tolerance is {Tolerance}",
                mrn,
                commodity.TaricCommodityCode,
                commodity.GoodsDescription,
                mrnWeight,
                totalWeight,
                difference,
                options.Value.QuantityManagementCheckNetMassTolerance
            );

            var mrnWeights = string.Join(
                ", ",
                mrnCommodities.Select(x => $"Item: {x.ItemNumber} - Code: {x.TaricCommodityCode} - Weight: {x.NetMass}")
            );

            var chedWeights = string.Join(
                ", ",
                commodities.Select(x => $"Code: {x.CommodityCode} - Weight: {x.Weight}")
            );

            logger.LogInformation("Weights used. MRN: [{MrnWeights}] CHED: [{ChedWeights}]", mrnWeights, chedWeights);
        }

        return difference >= 0;
    }
}
