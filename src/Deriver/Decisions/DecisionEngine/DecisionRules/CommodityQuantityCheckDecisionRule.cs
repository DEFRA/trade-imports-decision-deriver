using System.Runtime.CompilerServices;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public sealed class CommodityQuantityCheckDecisionRule : DecisionRule
{
    protected override DecisionEngineResult DoExecute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        var result = next(context);

        if (!result.Code.IsReleaseOrHold() || context.Level2Succeeded == false)
        {
            return result;
        }

        var mrnCommodity = context.Commodity;

        var chedCommodities = context
            .DecisionContext.Notifications.SelectMany(
                notification => notification.Commodities,
                (notification, commodity) => new NotificationCommodity(notification.Id, commodity)
            )
            .Where(x =>
                x.Commodity.CommodityCode != null
                && mrnCommodity.TaricCommodityCode?.StartsWith(x.Commodity.CommodityCode) == true
            )
            .ToList();

        var commodities =
            context.ClearanceRequest.CustomsDeclaration.ClearanceRequest?.Commodities ?? Enumerable.Empty<Commodity>();

        var mrnCommodities = commodities
            .Where(mrn =>
                chedCommodities.Any(ched => mrn.TaricCommodityCode?.StartsWith(ched.Commodity.CommodityCode!) == true)
            )
            .Where(mrn =>
                mrn.Documents != null
                && mrn.Documents.Any(d =>
                    d.GetDocumentReferenceIdentifier() == context.ImportDocument?.GetDocumentReferenceIdentifier()
                )
            )
            .ToList();

        var rule = GetBestMatchingRule(context, mrnCommodity.TaricCommodityCode);

        context.Logger.LogInformation(
            "Best matching rule for {Mrn}: {Rule}",
            context.ClearanceRequest.MovementReferenceNumber,
            rule
        );

        var validation = Validate(rule, context, mrnCommodity, mrnCommodities, chedCommodities);

        if (!validation.IsValid)
        {
            var detail = validation.ComparisonType switch
            {
                QuantityComparisonType.Weight => DecisionInternalFurtherDetail.E30,
                QuantityComparisonType.Quantity => DecisionInternalFurtherDetail.E31,
                _ => rule.ComparisonType == QuantityComparisonType.Weight
                    ? DecisionInternalFurtherDetail.E30
                    : DecisionInternalFurtherDetail.E31,
            };

            var liveResult = ApplyLevel3Result(result, detail, context.DecisionRulesOptions.Level3Mode);

            if (liveResult != null)
            {
                return liveResult;
            }
        }

        return result;
    }

    private ValidationResult Validate(
        CommodityQuantityCheckDecisionRuleComparisonEntry rule,
        DecisionEngineContext context,
        Commodity commodity,
        List<Commodity> mrnCommodities,
        List<NotificationCommodity> chedCommodities
    )
    {
        foreach (var comparison in GetComparisonOrder(rule))
        {
            switch (comparison)
            {
                case QuantityComparisonType.Weight when commodity.NetMass.HasValue:

                    return new ValidationResult(
                        WeightValid(
                            context.ClearanceRequest.MovementReferenceNumber,
                            commodity,
                            mrnCommodities,
                            chedCommodities,
                            context.DecisionRulesOptions.QuantityManagementCheckNetMassTolerance,
                            context.Logger
                        ),
                        QuantityComparisonType.Weight
                    );

                case QuantityComparisonType.Quantity when commodity.SupplementaryUnits.HasValue:

                    return new ValidationResult(
                        QuantityValid(
                            context.ClearanceRequest.MovementReferenceNumber,
                            commodity,
                            mrnCommodities,
                            chedCommodities,
                            context.Logger
                        ),
                        QuantityComparisonType.Quantity
                    );
            }
        }

        return new ValidationResult(false, null);
    }

    private static IEnumerable<QuantityComparisonType> GetComparisonOrder(
        CommodityQuantityCheckDecisionRuleComparisonEntry rule
    )
    {
        yield return rule.ComparisonType;

        if (!rule.UseFallback)
        {
            yield break;
        }

        yield return rule.ComparisonType == QuantityComparisonType.Weight
            ? QuantityComparisonType.Quantity
            : QuantityComparisonType.Weight;
    }

    private CommodityQuantityCheckDecisionRuleComparisonEntry GetBestMatchingRule(
        DecisionEngineContext context,
        string? commodityCode
    )
    {
        var chedType = context.Notification.ImportNotificationType;
        var checkCode = context.CheckCode.Value;

        var rule = context
            .DecisionRulesOptions.CommodityQuantityCheckDecisionRule.ComparisonEntries.Select(rule => new
            {
                Rule = rule,
                Score = CalculateScore(
                    context.DecisionRulesOptions.CommodityQuantityCheckDecisionRule.Scoring,
                    rule,
                    chedType,
                    checkCode,
                    commodityCode
                ),
            })
            .Where(x => x.Score >= 0)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Rule)
            .First();

        return rule;
    }

    private static int CalculateScore(
        CommodityQuantityCheckDecisionRuleScoringOptions scoring,
        CommodityQuantityCheckDecisionRuleComparisonEntry rule,
        string? chedType,
        string? checkCode,
        string? commodityCode
    )
    {
        var score = 0;

        if (rule.ChedType is not null && string.Equals(rule.ChedType, chedType, StringComparison.OrdinalIgnoreCase))
        {
            score += scoring.ChedTypeWeight;
        }

        if (rule.CheckCode is not null && string.Equals(rule.CheckCode, checkCode, StringComparison.OrdinalIgnoreCase))
        {
            score += scoring.CheckCodeWeight;
        }

        if (
            rule.CommodityCode is not null
            && string.Equals(rule.CommodityCode, commodityCode, StringComparison.OrdinalIgnoreCase)
        )
        {
            score += scoring.CommodityWeight;
        }

        return score;
    }

    private static DecisionEngineResult? ApplyLevel3Result(
        DecisionEngineResult result,
        DecisionInternalFurtherDetail furtherDetail,
        RuleMode mode
    ) =>
        mode switch
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
        List<NotificationCommodity> commodities,
        ILogger logger
    )
    {
        var chedQuantity = commodities.Sum(x => x.Commodity.Quantity);

        var mrnQuantity = mrnCommodities.Sum(x => x.SupplementaryUnits);

        var difference = chedQuantity - mrnQuantity;

        if (difference < 0)
        {
            logger.LogWarning(
                "{MRN} would not match at Level 3 due to a discrepancy on the quantity values. The item with the discrepancy is {TaricCommodityCode} & {GoodsDescription}, the quantity on the MRN is {ItemQuantity}, the quantity on the CHED is {ChedQuantity}, the difference is {Difference}",
                mrn,
                commodity.TaricCommodityCode,
                commodity.GoodsDescription,
                mrnQuantity,
                chedQuantity,
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
                commodities.Select(x =>
                    $"CHED: {x.Id} - Code: {x.Commodity.CommodityCode} - Quantity: {x.Commodity.Quantity}"
                )
            );

            logger.LogInformation(
                "Weights used. MRN: [{MrnQuantities}] CHED: [{ChedQuantities}]",
                mrnWeights,
                chedWeights
            );
        }

        return difference >= 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool WeightValid(
        string mrn,
        Commodity commodity,
        List<Commodity> mrnCommodities,
        List<NotificationCommodity> commodities,
        decimal? tolerance,
        ILogger logger
    )
    {
        var totalWeight = commodities.Sum(x => x.Commodity.Weight) ?? 0m;
        var mrnWeight = mrnCommodities.Sum(x => x.NetMass);

        var chedWeight = totalWeight + tolerance;

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
                tolerance
            );

            var mrnWeights = string.Join(
                ", ",
                mrnCommodities.Select(x => $"Item: {x.ItemNumber} - Code: {x.TaricCommodityCode} - Weight: {x.NetMass}")
            );

            var chedWeights = string.Join(
                ", ",
                commodities.Select(x =>
                    $"CHED: {x.Id} - Code: {x.Commodity.CommodityCode} - Weight: {x.Commodity.Weight}"
                )
            );

            logger.LogInformation("Weights used. MRN: [{MrnWeights}] CHED: [{ChedWeights}]", mrnWeights, chedWeights);
        }

        return difference >= 0;
    }

    private readonly record struct ValidationResult(bool IsValid, QuantityComparisonType? ComparisonType);

    private readonly record struct NotificationCommodity(string Id, DecisionCommodityComplement Commodity);
}
