using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;

public sealed record DecisionEngineContext(
    DecisionContext DecisionContext,
    DecisionImportPreNotification Notification,
    CustomsDeclarationWrapper ClearanceRequest,
    Commodity Commodity,
    CheckCode CheckCode,
    ImportDocument? ImportDocument
)
{
    public ILogger Logger { get; set; } = null!;
}
