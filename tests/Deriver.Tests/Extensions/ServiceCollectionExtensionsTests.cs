using System.Text.Json;
using System.Text.Json.Serialization;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    private static JsonSerializerOptions GetSerializerOptions()
    {
        var services = new ServiceCollection();
        services.AddJsonSerialization();

        return services.BuildServiceProvider().GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;
    }

    [Fact]
    public void AddJsonSerialization_ShouldUseCamelCasePropertyNames()
    {
        GetSerializerOptions().PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);
    }

    [Fact]
    public void AddJsonSerialization_ShouldSerializeEnumsAsStrings()
    {
        GetSerializerOptions().Converters.Should().ContainSingle(c => c is JsonStringEnumConverter);
    }

    [Fact]
    public void AddJsonSerialization_ShouldNotChangeDictionaryKeys()
    {
        var json = JsonSerializer.Serialize(
            new { SomeProperty = new Dictionary<string, int> { ["CHEDA"] = 1 } },
            GetSerializerOptions()
        );

        json.Should().Be("{\"someProperty\":{\"CHEDA\":1}}");
    }
}
