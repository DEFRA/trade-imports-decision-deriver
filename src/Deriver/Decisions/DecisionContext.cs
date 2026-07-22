using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Trade.Gateway.Api.Contract.Certificate;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

public class DecisionContext(
    List<DecisionImportPreNotification> notifications,
    List<CustomsDeclarationWrapper> customsDeclarations,
    List<DefraUNVTDCHEDProfile> cheds
)
{
    public List<DecisionImportPreNotification> Notifications { get; } = notifications;
    public List<CustomsDeclarationWrapper> CustomsDeclarations { get; } = customsDeclarations;
    public List<DefraUNVTDCHEDProfile> Cheds { get; } = cheds;
}
