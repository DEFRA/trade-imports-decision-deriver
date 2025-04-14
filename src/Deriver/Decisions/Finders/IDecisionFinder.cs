using Defra.TradeImportsDataApi.Domain.Ipaffs;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

public interface IDecisionFinder
{
    bool CanFindDecision(ImportPreNotification notification, CheckCode? checkCode);
    DecisionFinderResult FindDecision(ImportPreNotification notification, CheckCode? checkCode);
}