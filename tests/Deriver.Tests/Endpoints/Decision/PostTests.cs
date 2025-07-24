using System.Net;
using System.Net.Http.Json;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Endpoints.Decision;
using Defra.TradeImportsDecisionDeriver.Deriver.Utils.CorrelationId;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using Defra.TradeImportsDecisionDeriver.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using WireMock.Server;
using Xunit.Abstractions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Endpoints.Decision;

public class PostTests : EndpointTestBase, IClassFixture<WireMockContext>
{
    private ITradeImportsDataApiClient MockTradeImportsDataApiClient { get; } =
        Substitute.For<ITradeImportsDataApiClient>();
    private ICorrelationIdGenerator MockCorrelationIdGenerator { get; } = Substitute.For<ICorrelationIdGenerator>();
    private WireMockServer WireMock { get; }
    private const string Mrn = "mrn";
    private readonly VerifySettings _settings;

    public PostTests(ApiWebApplicationFactory factory, ITestOutputHelper outputHelper, WireMockContext context)
        : base(factory, outputHelper)
    {
        WireMock = context.Server;
        WireMock.Reset();

        _settings = new VerifySettings();
        _settings.ScrubMember("created");
        _settings.ScrubMember("traceId");
        _settings.DontScrubDateTimes();
        _settings.DontScrubGuids();
        _settings.DontIgnoreEmptyCollections();
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);

        services.AddTransient<ITradeImportsDataApiClient>(_ => MockTradeImportsDataApiClient);
        services.AddTransient<ICorrelationIdGenerator>(_ => MockCorrelationIdGenerator);
    }

    protected override void ConfigureHostConfiguration(IConfigurationBuilder config)
    {
        base.ConfigureHostConfiguration(config);

        config.AddInMemoryCollection([new KeyValuePair<string, string?>("AUTO_START_CONSUMERS", "false")]);
    }

    [Fact]
    public async Task Get_WhenNotFound_ShouldNotBeFound()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            Testing.Endpoints.Decision.Post(Mrn),
            new DecisionRequest(PersistOption.AlwaysPersist)
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), _settings);
    }

    [Fact]
    public async Task Get_WhenFound_ShouldReturnContent()
    {
        var client = CreateClient();
        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseSimpleStaticFixture();

        MockCorrelationIdGenerator.Generate().Returns("TestCorrelationId");

        MockTradeImportsDataApiClient
            .GetCustomsDeclaration(customsDeclaration.MovementReferenceNumber, Arg.Any<CancellationToken>())
            .Returns(customsDeclaration);

        MockTradeImportsDataApiClient
            .GetImportPreNotificationsByMrn(customsDeclaration.MovementReferenceNumber, Arg.Any<CancellationToken>())
            .Returns(new ImportPreNotificationsResponse([]));

        var response = await client.PostAsJsonAsync(
            Testing.Endpoints.Decision.Post(customsDeclaration.MovementReferenceNumber),
            new DecisionRequest(PersistOption.AlwaysPersist)
        );

        await MockTradeImportsDataApiClient
            .Received(1)
            .PutCustomsDeclaration(
                customsDeclaration.MovementReferenceNumber,
                Arg.Any<CustomsDeclaration>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            );
        await VerifyJson(await response.Content.ReadAsStringAsync(), _settings);
    }

    [Fact]
    public async Task Get_WhenUnauthorized_ShouldBeUnauthorized()
    {
        var client = CreateClient(addDefaultAuthorizationHeader: false);

        var response = await client.PostAsJsonAsync(
            Testing.Endpoints.Decision.Post(Mrn),
            new DecisionRequest(PersistOption.AlwaysPersist)
        );

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_WhenWriteOnly_ShouldBeForbidden()
    {
        var client = CreateClient(testUser: TestUser.ReadOnly);

        var response = await client.PostAsJsonAsync(
            Testing.Endpoints.Decision.Post(Mrn),
            new DecisionRequest(PersistOption.AlwaysPersist)
        );

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
