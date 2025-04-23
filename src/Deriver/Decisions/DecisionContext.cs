using Defra.TradeImportsDecisionDeriver.Deriver.Matching;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

public class DecisionContext(
    List<DecisionImportPreNotification> notifications,
    List<ClearanceRequestWrapper> clearanceRequests
)
{
    public List<DecisionImportPreNotification> Notifications { get; } = notifications;
    public List<ClearanceRequestWrapper> ClearanceRequests { get; } = clearanceRequests;
}
