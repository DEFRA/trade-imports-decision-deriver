using System.Diagnostics.CodeAnalysis;
using System.Text;
using SlimMessageBus.Host.Serialization;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Serializers;

public class ToStringSerializer : IMessageSerializer, IMessageSerializer<string>, IMessageSerializerProvider
{
    [ExcludeFromCodeCoverage]
    string IMessageSerializer<string>.Serialize(
        Type messageType,
        IDictionary<string, object> headers,
        object message,
        object transportMessage
    )
    {
        return message.ToString()!;
    }

    [ExcludeFromCodeCoverage]
    object IMessageSerializer<string>.Deserialize(
        Type messageType,
        IReadOnlyDictionary<string, object> headers,
        string payload,
        object transportMessage
    )
    {
        return payload;
    }

    [ExcludeFromCodeCoverage]
    byte[] IMessageSerializer<byte[]>.Serialize(
        Type messageType,
        IDictionary<string, object> headers,
        object message,
        object transportMessage
    )
    {
        throw new NotImplementedException();
    }

    [ExcludeFromCodeCoverage]
    object IMessageSerializer<byte[]>.Deserialize(
        Type messageType,
        IReadOnlyDictionary<string, object> headers,
        byte[] payload,
        object transportMessage
    )
    {
        return Encoding.UTF8.GetString(payload);
    }

    [ExcludeFromCodeCoverage]
    public IMessageSerializer GetSerializer(string path) => this;
}
