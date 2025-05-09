using RestEase;
using WireMock.Client;

namespace Defra.TradeImportsDecisionDeriver.Deriver.IntegrationTests.Clients;

public class WireMockClient
{
    public WireMockClient()
    {
        WireMockAdminApi.ResetMappingsAsync().GetAwaiter().GetResult();
        WireMockAdminApi.ResetRequestsAsync().GetAwaiter().GetResult();
    }

    public IWireMockAdminApi WireMockAdminApi { get; } = RestClient.For<IWireMockAdminApi>("http://localhost:9090");
}

[CollectionDefinition("UsesWireMockClient")]
public class WireMockClientCollection : ICollectionFixture<WireMockClient>;
