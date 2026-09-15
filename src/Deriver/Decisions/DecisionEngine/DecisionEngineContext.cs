using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Trade.Gateway.Api.Contract.Certificate;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;

public sealed record DecisionEngineContext(
    DecisionContext DecisionContext,
    DecisionRulesOptions DecisionRulesOptions,
    DecisionImportPreNotification Notification,
    CustomsDeclarationWrapper ClearanceRequest,
    Commodity Commodity,
    CheckCode CheckCode,
    ImportDocument? ImportDocument,
    DefraUNVTDCHEDProfile? Ched
)
{
    public string? Source { get; set; }
    public ILogger Logger { get; set; } = null!;

    public bool? Level2Succeeded { get; set; }

    public bool? Level3Succeeded { get; set; }

    public DecisionRulesSourceOptions GetDecisionRulesSourceOptions() =>
        Source == Constants.ChedSource.Ipaffs ? DecisionRulesOptions.Ipaffs : DecisionRulesOptions.Traces;
}
