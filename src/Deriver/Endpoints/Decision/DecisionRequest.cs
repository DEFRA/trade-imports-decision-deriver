using System.Text.Json.Serialization;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Endpoints.Decision;

public record DecisionRequest([property: JsonPropertyName("persistOption")] PersistOption PersistOption);
