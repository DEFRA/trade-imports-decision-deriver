namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

public interface IDecisionFinder
{
    string ChedType { get; }
    bool CanFindDecision(DecisionImportPreNotification notification, CheckCode? checkCode, string? documentCode);
    DecisionFinderResult FindDecision(DecisionImportPreNotification notification, CheckCode? checkCode);
}
