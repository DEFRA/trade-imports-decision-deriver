using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;
using Microsoft.Extensions.Options;
using Trade.Gateway.Api.Contract.Certificate;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces;

public sealed class TracesCommodityCodeDecisionRule(IOptions<DecisionRulesOptions> options) : DecisionRule
{
    protected override DecisionEngineResult DoExecute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        var result = next(context);

        if (!result.Code.IsReleaseOrHold())
        {
            return result;
        }

        var commodity = context.Commodity;
        var chedCommodityCodes = GetChedCommodityCodes(context.Ched);

        context.Level2Succeeded = chedCommodityCodes.Any(code =>
            code != null && commodity.TaricCommodityCode?.StartsWith(code) == true
        );

        if (context.Level2Succeeded == false)
        {
            switch (options.Value.Level2Mode)
            {
                case RuleMode.DryRun:
                    result.AddResult(
                        new DecisionEngineResult(
                            DecisionCode.X00,
                            nameof(TracesCommodityCodeDecisionRule),
                            DecisionInternalFurtherDetail.E20,
                            DecisionResultMode.Passive,
                            DecisionRuleLevel.Level2
                        )
                    );
                    break;
                case RuleMode.Live:
                    return new DecisionEngineResult(
                        DecisionCode.X00,
                        nameof(TracesCommodityCodeDecisionRule),
                        DecisionInternalFurtherDetail.E20,
                        Level: DecisionRuleLevel.Level2
                    );
            }
        }

        return result;
    }

    private static List<string?> GetChedCommodityCodes(DefraUNVTDCHEDProfile? ched)
    {
        var consignmentItems = ched?.SpecifiedConsignment?.IncludedConsignmentItem ?? [];

        return consignmentItems
            .SelectMany(item => item.IncludedTradeLineItem ?? [])
            .SelectMany(tradeLineItem => tradeLineItem.ApplicableClassification ?? [])
            .Select(classification => classification.ClassCode?.Value)
            .ToList();
    }
}
