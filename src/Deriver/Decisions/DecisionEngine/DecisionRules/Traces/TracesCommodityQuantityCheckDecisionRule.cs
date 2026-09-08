using System.Globalization;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;
using Trade.Gateway.Api.Contract.Certificate;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces;

public sealed class TracesCommodityQuantityCheckDecisionRule : CommodityQuantityCheckRuleBase
{
    private static readonly Dictionary<string, decimal> s_weightConversionFactorsToKgm = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["KGM"] = 1m,
        ["GRM"] = 0.001m,
        ["TNE"] = 1000m,
        ["DTN"] = 100m,
    };

    protected override string? GetChedType(DecisionEngineContext context) => context.Ched?.Type;

    protected override void OnCompleted(DecisionEngineContext context) => context.Level3Succeeded = true;

    protected override IEnumerable<ChedQuantityCommodity> GetChedCommodities(DecisionEngineContext context)
    {
        var consignmentItems = context.Ched?.SpecifiedConsignment?.IncludedConsignmentItem ?? [];

        return consignmentItems
            .SelectMany(item => item.IncludedTradeLineItem ?? [])
            .Select(tradeLineItem => new ChedQuantityCommodity(
                tradeLineItem
                    .ApplicableClassification?.Select(classification => classification.ClassCode?.Value)
                    .FirstOrDefault(value => !string.IsNullOrEmpty(value)),
                ConvertWeightToKgm(tradeLineItem.NetWeight),
                tradeLineItem
                    .PhysicalReferencedLogisticsPackage?.Where(package => package.ItemQuantity.HasValue)
                    .Sum(package => (decimal?)package.ItemQuantity)
            ));
    }

    private static decimal? ConvertWeightToKgm(UneceWeightMeasure? weight)
    {
        var value = ParseDecimal(weight?.Value);

        if (value == null)
        {
            return null;
        }

        if (weight?.UnitCode == null)
        {
            return value;
        }

        return s_weightConversionFactorsToKgm.TryGetValue(weight.UnitCode, out var factor) ? value * factor : null;
    }

    private static decimal? ParseDecimal(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;
}
