using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Matching;

public record CustomsDeclarationWrapper(string MovementReferenceNumber, CustomsDeclaration CustomsDeclaration);
