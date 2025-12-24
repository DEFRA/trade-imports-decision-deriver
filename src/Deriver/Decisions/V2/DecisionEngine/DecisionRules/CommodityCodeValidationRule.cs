using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine.DecisionRules;

public sealed class CommodityCodeValidationRule : IDecisionRule
{
    public DecisionResolutionResult Execute(DecisionResolutionContext context, DecisionRuleDelegate next)
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

        if (commodities.Count == 0)
        {
            context.Logger.LogWarning(
                "Level 2 would have resulted in an X00 as could not match CommodityCode {CommodityCode} for Item {Item}",
                commodity.TaricCommodityCode,
                commodity.ItemNumber
            );
        }

        return result;
    }
}
