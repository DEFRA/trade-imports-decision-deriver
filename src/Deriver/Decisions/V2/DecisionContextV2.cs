using Defra.TradeImportsDecisionDeriver.Deriver.Matching;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2;

public class DecisionContextV2(
    List<DecisionImportPreNotification> notifications,
    List<CustomsDeclarationWrapper> customsDeclarations
)
{
    public List<DecisionImportPreNotification> Notifications { get; } = notifications;
    public List<CustomsDeclarationWrapper> CustomsDeclarations { get; } = customsDeclarations;
}
