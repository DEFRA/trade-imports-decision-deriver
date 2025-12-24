using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Matching;

public class MatchingContext(
    List<DecisionImportPreNotification> notifications,
    List<ClearanceRequestWrapper> clearanceRequests
)
{
    public List<DecisionImportPreNotification> Notifications { get; } = notifications;
    public List<ClearanceRequestWrapper> ClearanceRequests { get; } = clearanceRequests;
}

public record ClearanceRequestWrapper(string MovementReferenceNumber, ClearanceRequest ClearanceRequest)
{
    public string GetVersion()
    {
        return $"{MovementReferenceNumber}_{ClearanceRequest.GetVersion()}";
    }
}

public record CustomsDeclarationWrapper(string MovementReferenceNumber, CustomsDeclaration CustomsDeclaration)
{
    public string GetVersion()
    {
        return $"{MovementReferenceNumber}_{CustomsDeclaration.ClearanceRequest.GetVersion()}";
    }
}
