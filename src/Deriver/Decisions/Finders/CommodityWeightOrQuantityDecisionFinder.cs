using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

public class CommodityWeightOrQuantityDecisionFinder(
    IDecisionFinder innerDecisionFinder,
    ILogger<CommodityWeightOrQuantityDecisionFinder> logger
) : IDecisionFinder
{
    public Type FinderType => innerDecisionFinder.FinderType;
    public string ChedType => innerDecisionFinder.ChedType;

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
        logger.LogDebug(nameof(CommodityWeightOrQuantityDecisionFinder));
        return innerDecisionFinder.FindDecision(notification, commodity, checkCode);
    }
}
