using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Trade.Gateway.Api.Contract.Certificate;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;

public sealed record DecisionEngineContext(
    DecisionContext DecisionContext,
    DecisionImportPreNotification Notification,
    CustomsDeclarationWrapper ClearanceRequest,
    Commodity Commodity,
    CheckCode CheckCode,
    ImportDocument? ImportDocument,
    DefraUNVTDCHEDProfile? Ched
)
{
    public ILogger Logger { get; set; } = null!;

    public bool? Level2Succeeded { get; set; }
}
