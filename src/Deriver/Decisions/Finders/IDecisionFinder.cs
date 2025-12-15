using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

public interface IDecisionFinder
{
    Type FinderType { get; }
    string ChedType { get; }
    bool CanFindDecision(DecisionImportPreNotification notification, CheckCode? checkCode, string? documentCode);
    DecisionFinderResult FindDecision(
        DecisionImportPreNotification notification,
        Commodity commodity,
        CheckCode? checkCode
    );
}
