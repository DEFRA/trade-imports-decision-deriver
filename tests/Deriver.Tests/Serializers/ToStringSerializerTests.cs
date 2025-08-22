using System.Text;
using Defra.TradeImportsDecisionDeriver.Deriver.Serializers;
using SlimMessageBus.Host.Serialization;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Serializers;

public class ToStringSerializerTests
{
    private readonly ToStringSerializer _toStringSerializer = new();

    [Fact]
    public void Deserialize_String_Returns_String()
    {
        ((IMessageSerializer<string>)_toStringSerializer)
            .Deserialize(null!, null!, "sosig", null!)
            .Should()
            .Be("sosig");
    }

    [Fact]
    public void Deserialize_Byte_ThrowsNotImplementedException()
    {
        ((IMessageSerializer<byte[]>)_toStringSerializer)
            .Deserialize(null!, null!, "sosig"u8.ToArray(), null!)
            .Should()
            .Be("sosig");
    }
}
