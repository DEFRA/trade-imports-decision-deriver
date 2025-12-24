using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine;

public sealed record DecisionResolutionContext(
    DecisionContextV2 DecisionContext,
    DecisionImportPreNotification Notification,
    CustomsDeclarationWrapper ClearanceRequest,
    Commodity Commodity,
    CheckCode CheckCode,
    ImportDocument? ImportDocument
)
{
    public ILogger Logger { get; set; } = null!;
}
