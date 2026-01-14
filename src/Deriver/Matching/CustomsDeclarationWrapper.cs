using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Matching;

public record CustomsDeclarationWrapper(string MovementReferenceNumber, CustomsDeclaration CustomsDeclaration)
{
    public string GetVersion()
    {
        return $"{MovementReferenceNumber}_{CustomsDeclaration.ClearanceRequest.GetVersion()}";
    }
}
