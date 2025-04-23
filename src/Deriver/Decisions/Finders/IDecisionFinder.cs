namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

public interface IDecisionFinder
{
    bool CanFindDecision(DecisionImportPreNotification notification, CheckCode? checkCode);
    DecisionFinderResult FindDecision(DecisionImportPreNotification notification, CheckCode? checkCode);
}
