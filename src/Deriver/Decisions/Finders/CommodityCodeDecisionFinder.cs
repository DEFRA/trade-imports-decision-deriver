using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;

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
        logger.LogDebug(nameof(CommodityCodeDecisionFinder));
        return innerDecisionFinder.FindDecision(notification, commodity, checkCode);
    }
}
