using Defra.TradeImportsDecisionDeriver.Deriver.Matching;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

public class DecisionContext(
    List<DecisionImportPreNotification> notifications,
    List<CustomsDeclarationWrapper> customsDeclarations
)
{
    public List<DecisionImportPreNotification> Notifications { get; } = notifications;
    public List<CustomsDeclarationWrapper> CustomsDeclarations { get; } = customsDeclarations;
}
