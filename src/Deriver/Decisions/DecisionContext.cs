using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

public class DecisionContext(List<ImportPreNotification> notifications, List<ClearanceRequestWrapper> clearanceRequests)
{
    public List<ImportPreNotification> Notifications { get; } = notifications;
    public List<ClearanceRequestWrapper> ClearanceRequests { get; } = clearanceRequests;
}
