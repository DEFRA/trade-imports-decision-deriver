using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Matching;

public record ClearanceRequestWrapper(string MovementReferenceNumber, ClearanceRequest ClearanceRequest);
