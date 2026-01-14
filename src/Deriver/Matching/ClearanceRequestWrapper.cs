using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Matching;

public record ClearanceRequestWrapper(string MovementReferenceNumber, ClearanceRequest ClearanceRequest)
{
    public string GetVersion()
    {
        return $"{MovementReferenceNumber}_{ClearanceRequest.GetVersion()}";
    }
}
