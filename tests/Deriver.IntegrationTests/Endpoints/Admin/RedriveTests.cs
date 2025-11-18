using System.Net;
using System.Text.Json;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDecisionDeriver.Deriver.IntegrationTests.Clients;
using Defra.TradeImportsDecisionDeriver.Deriver.IntegrationTests.TestUtils;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using FluentAssertions;
using WireMock.Client;
using WireMock.Client.Extensions;
using Xunit.Abstractions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.IntegrationTests.Endpoints.Admin;

[Collection("UsesWireMockClient")]
public class RedriveTests(WireMockClient wireMockClient, ITestOutputHelper output) : AdminTestBase(output)
{
    private readonly IWireMockAdminApi _wireMockAdminApi = wireMockClient.WireMockAdminApi;

    [Fact]
    public async Task When_message_processing_fails_and_moved_to_dlq_Then_message_can_be_redriven()
    {
        const string mrn = "25GB0XX00XXXXX0000";
        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseSimpleStaticFixture(mrn);
        var resourceEvent = FixtureTest.UsingContent("CustomsDeclarationClearanceDecisionResourceEvent.json");

        var mappingBuilder = _wireMockAdminApi.GetMappingBuilder();

        mappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingGet().WithPath($"/customs-declarations/{mrn}"))
                .WithResponse(rsp =>
                    rsp.WithBody(JsonSerializer.Serialize(customsDeclaration)).WithStatusCode(HttpStatusCode.OK)
                )
        );

        mappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingGet().WithPath($"/customs-declarations/{mrn}/import-pre-notifications"))
                .WithResponse(rsp =>
                    rsp.WithBody(JsonSerializer.Serialize(new ImportPreNotificationsResponse([])))
                        .WithStatusCode(HttpStatusCode.OK)
                )
        );

        mappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingPut().WithPath($"/customs-declarations/{mrn}"))
                .WithResponse(rsp => rsp.WithStatusCode(HttpStatusCode.OK))
        );

        var getMappingBuilderResult = await mappingBuilder.BuildAndPostAsync();
        Assert.Null(getMappingBuilderResult.Error);

        await PurgeQueue(QueueUrl);
        await PurgeQueue(DeadLetterQueueUrl);

        await SendMessage(
            mrn,
            resourceEvent,
            DeadLetterQueueUrl,
            WithResourceEventAttributes("CustomsDeclaration", "ClearanceDecision", mrn),
            false
        );

        var messagesOnDeadLetterQueue = await AsyncWaiter.WaitForAsync(async () =>
            (await GetQueueAttributes(DeadLetterQueueUrl)).ApproximateNumberOfMessages == 1
        );
        Assert.True(messagesOnDeadLetterQueue, "Messages on dead letter queue was not received");

        var httpClient = CreateHttpClient();
        var response = await httpClient.PostAsync(Testing.Endpoints.Admin.DeadLetterQueue.Redrive(), null);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
                (await GetQueueAttributes(QueueUrl)).ApproximateNumberOfMessages == 0
            )
        );

        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
                (await GetQueueAttributes(DeadLetterQueueUrl)).ApproximateNumberOfMessages == 0
            )
        );
    }

    [Fact]
    public async Task When_message_processing_fails_and_moved_to_dlq_Then_dlq_can_be_drained()
    {
        var resourceEvent = FixtureTest.UsingContent("CustomsDeclarationClearanceDecisionResourceEvent.json");
        const string mrn = "25GB0XX00XXXXX0002";
        resourceEvent = resourceEvent.Replace("25GB0XX00XXXXX0000", mrn);

        ////await SetUpConsumptionFailure(_wireMockAdminApi, "DLQ Drain", mrn);
        await PurgeQueue(QueueUrl);
        await PurgeQueue(DeadLetterQueueUrl);

        await SendMessage(
            mrn,
            resourceEvent,
            DeadLetterQueueUrl,
            WithResourceEventAttributes("CustomsDeclaration", "ClearanceDecision", mrn),
            false
        );

        var messagesOnDeadLetterQueue = await AsyncWaiter.WaitForAsync(async () =>
            (await GetQueueAttributes(DeadLetterQueueUrl)).ApproximateNumberOfMessages == 1
        );
        Assert.True(messagesOnDeadLetterQueue, "Messages on dead letter queue was not received");

        var httpClient = CreateHttpClient();
        var response = await httpClient.PostAsync(Testing.Endpoints.Admin.DeadLetterQueue.Drain(), null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // We expect no messages on either queue following a drain
        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
                (await GetQueueAttributes(QueueUrl)).ApproximateNumberOfMessages == 0
            )
        );
        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
                (await GetQueueAttributes(DeadLetterQueueUrl)).ApproximateNumberOfMessages == 0
            )
        );
    }

    [Fact]
    public async Task When_message_processing_fails_and_moved_to_dlq_Then_message_can_be_removed()
    {
        var resourceEvent = FixtureTest.UsingContent("CustomsDeclarationClearanceDecisionResourceEvent.json");
        const string mrn = "25GB0XX00XXXXX0001";
        resourceEvent = resourceEvent.Replace("25GB0XX00XXXXX0000", mrn);

        await PurgeQueue(QueueUrl);
        await PurgeQueue(DeadLetterQueueUrl);

        var messageId = await SendMessage(
            mrn,
            resourceEvent,
            DeadLetterQueueUrl,
            WithResourceEventAttributes("CustomsDeclaration", "ClearanceDecision", mrn),
            false
        );

        var messagesOnDeadLetterQueue = await AsyncWaiter.WaitForAsync(async () =>
            (await GetQueueAttributes(DeadLetterQueueUrl)).ApproximateNumberOfMessages == 1
        );
        Assert.True(messagesOnDeadLetterQueue, "Messages on dead letter queue was not received");

        var httpClient = CreateHttpClient();
        var response = await httpClient.PostAsync(
            Testing.Endpoints.Admin.DeadLetterQueue.RemoveMessage(messageId),
            null
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // We expect no messages on either queue following removal of the single message
        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
                (await GetQueueAttributes(QueueUrl)).ApproximateNumberOfMessages == 0
            )
        );
        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
                (await GetQueueAttributes(DeadLetterQueueUrl)).ApproximateNumberOfMessages == 0
            )
        );
    }
}
