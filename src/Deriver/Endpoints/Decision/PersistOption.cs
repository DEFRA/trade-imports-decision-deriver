using System.Text.Json.Serialization;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Endpoints.Decision;

[JsonConverter(typeof(JsonStringEnumConverter<PersistOption>))]
public enum PersistOption
{
    DoNotPersist,
    AlwaysPersist,
    PersistIfSame,
}
