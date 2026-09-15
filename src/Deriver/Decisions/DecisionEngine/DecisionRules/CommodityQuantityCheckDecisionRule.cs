namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public sealed class CommodityQuantityCheckDecisionRule : CommodityQuantityCheckRuleBase
{
    protected override string? GetChedType(DecisionEngineContext context) =>
        context.Notification.ImportNotificationType;

    protected override IEnumerable<ChedQuantityCommodity> GetChedCommodities(DecisionEngineContext context)
    {
        return context
            .DecisionContext.Notifications.SelectMany(notification => notification.Commodities)
            .Select(commodity => new ChedQuantityCommodity(
                commodity.CommodityCode,
                commodity.Weight,
                commodity.Quantity
            ));
    }
}
