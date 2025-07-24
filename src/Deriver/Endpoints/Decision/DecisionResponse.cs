using System.Text.Json.Serialization;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Endpoints.Decision;

public record DecisionResponse(
    [property: JsonPropertyName("isDifferent")] bool IsDifferent,
    [property: JsonPropertyName("persisted")] bool Persisted,
    [property: JsonPropertyName("clearanceDecision")] ClearanceDecision ClearanceDecision
);
