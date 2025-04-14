using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Matching;

public class MatchingContext(List<ImportPreNotification> notifications, List<ClearanceRequestWrapper> clearanceRequests)
{
    public List<ImportPreNotification> Notifications { get; } = notifications;
    public List<ClearanceRequestWrapper> ClearanceRequests { get; } = clearanceRequests;
}

public record ClearanceRequestWrapper(string MovementReferenceNumber, ClearanceRequest ClearanceRequest);
