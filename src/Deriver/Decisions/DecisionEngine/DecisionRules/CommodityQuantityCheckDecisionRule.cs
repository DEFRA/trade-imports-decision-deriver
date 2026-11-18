namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public sealed class CommodityQuantityCheckDecisionRule : CommodityQuantityCheckRuleBase
{
    protected override string? GetChedType(DecisionEngineContext context) =>
        context.Notification.ImportNotificationType;

    protected override IEnumerable<ChedQuantityCommodity> GetChedCommodities(DecisionEngineContext context)
    {
        // Only the CHED this item references: every notification on the MRN is in the DecisionContext, and
        // summing them would let one CHED's weight/quantity cover items declared against another
        return context.Notification.Commodities.Select(commodity => new ChedQuantityCommodity(
            commodity.CommodityCode,
            commodity.Weight,
            commodity.Quantity
        ));
    }
}
