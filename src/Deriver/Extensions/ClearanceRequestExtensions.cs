using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

public static class ClearanceRequestExtensions
{
    public static string GetVersion(this ClearanceRequest? clearanceRequest)
    {
        return $"{clearanceRequest?.ExternalVersion}_{clearanceRequest?.MessageSentAt:ddMMyyhhmmssms}";
    }
}
