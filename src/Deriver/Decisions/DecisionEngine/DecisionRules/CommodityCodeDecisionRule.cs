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
                    context.Logger.LogWarning(
                        "Level 2 would have resulted in an X00 as could not match MRN {Mrn} CommodityCode {CommodityCode} for Item {Item}",
                        context.ClearanceRequest.MovementReferenceNumber,
                        commodity.TaricCommodityCode,
                        commodity.ItemNumber
                    );
                    break;
                case RuleMode.Live:
                    return DecisionEngineResult.X00E20;
            }
        }

        return result;
    }
}
