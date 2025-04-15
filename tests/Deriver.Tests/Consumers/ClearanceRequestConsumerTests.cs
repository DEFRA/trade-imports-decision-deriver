using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDecisionDeriver.Deriver.Consumers;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Consumers;

public class ClearanceRequestConsumerTests
{
    [Fact]
    public void OnHandle_ReturnsTaskCompleted()
    {
        var apiClient = Substitute.For<ITradeImportsDataApiClient>();
        var decisionService = Substitute.For<IDecisionService>();
        var consumer = new ClearanceRequestConsumer(
            NullLogger<ClearanceRequestConsumer>.Instance,
            decisionService,
            apiClient
        );

        var createdEvent = ClearanceRequestFixtures.ClearanceRequestCreatedFixture();
        var apiResponse = new CustomsDeclarationResponse(
            createdEvent.ResourceId,
            ClearanceRequestFixtures.ClearanceRequestFixture(),
            null,
            null,
            DateTime.Now,
            DateTime.Now
        );

        _ = apiClient.GetCustomsDeclaration(createdEvent.ResourceId, CancellationToken.None).Returns(apiResponse);

        var result = consumer.OnHandle(createdEvent, CancellationToken.None);
        Assert.Equal(Task.CompletedTask, result);
    }
}
