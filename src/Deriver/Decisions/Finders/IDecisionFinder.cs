namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

public interface IDecisionFinder
{
    bool CanFindDecision(DecisionImportPreNotification notification, CheckCode? checkCode, string? documentCode);
    DecisionFinderResult FindDecision(DecisionImportPreNotification notification, CheckCode? checkCode);
}
