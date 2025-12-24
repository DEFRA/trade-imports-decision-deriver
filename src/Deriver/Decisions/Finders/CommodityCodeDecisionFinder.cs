using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

public class CommodityCodeDecisionFinder(
    IDecisionFinder innerDecisionFinder,
    ILogger<CommodityCodeDecisionFinder> logger
) : IDecisionFinder
{
    public string ChedType => innerDecisionFinder.ChedType;
    public Type FinderType => innerDecisionFinder.FinderType;

    public bool CanFindDecision(DecisionImportPreNotification notification, CheckCode? checkCode, string? documentCode)
    {
        return innerDecisionFinder.CanFindDecision(notification, checkCode, documentCode);
    }

    public DecisionFinderResult FindDecision(
        DecisionImportPreNotification notification,
        Commodity commodity,
        CheckCode? checkCode
    )
    {
        var result = innerDecisionFinder.FindDecision(notification, commodity, checkCode);

        if (result.DecisionCode.IsReleaseOrHold())
        {
            var commodities = notification
                .Commodities.Where(x =>
                    x.CommodityCode != null && commodity.TaricCommodityCode?.StartsWith(x.CommodityCode) == true
                )
                .ToList();

            //check commodity code
            if (commodities.Count == 0)
            {
                logger.LogWarning(
                    "Level 2 would have resulted in an X00 as could not match CommodityCode {CommodityCode} for Item {Item}",
                    commodity.TaricCommodityCode,
                    commodity.ItemNumber
                );
            }
        }

        return result;
    }
}
