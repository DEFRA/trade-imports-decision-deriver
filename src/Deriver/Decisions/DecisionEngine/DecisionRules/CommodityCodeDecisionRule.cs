using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;
using Microsoft.Extensions.Options;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public sealed class CommodityCodeDecisionRule(IOptions<DecisionRulesOptions> options) : IDecisionRule
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

        context.Level2Succeeded = commodities.Count > 0;

        if (commodities.Count == 0)
        {
            switch (options.Value.Level2Mode)
            {
                case RuleMode.DryRun:
                    result.AddResult(
                        new DecisionEngineResult(
                            DecisionCode.X00,
                            nameof(CommodityCodeDecisionRule),
                            DecisionInternalFurtherDetail.E20,
                            DecisionResultMode.Passive,
                            DecisionRuleLevel.Level2
                        )
                    );
                    break;
                case RuleMode.Live:
                    return new DecisionEngineResult(
                        DecisionCode.X00,
                        nameof(CommodityCodeDecisionRule),
                        DecisionInternalFurtherDetail.E20,
                        Level: DecisionRuleLevel.Level2
                    );
            }
        }

        return result;
    }
}
