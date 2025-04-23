using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Matching;

public class MatchingContext(
    List<DecisionImportPreNotification> notifications,
    List<ClearanceRequestWrapper> clearanceRequests
)
{
    public List<DecisionImportPreNotification> Notifications { get; } = notifications;
    public List<ClearanceRequestWrapper> ClearanceRequests { get; } = clearanceRequests;
}

public record ClearanceRequestWrapper(string MovementReferenceNumber, ClearanceRequest ClearanceRequest);
