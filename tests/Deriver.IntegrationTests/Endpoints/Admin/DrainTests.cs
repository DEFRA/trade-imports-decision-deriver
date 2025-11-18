using System.Diagnostics.CodeAnalysis;
using System.Net;
using Defra.TradeImportsDecisionDeriver.Deriver.IntegrationTests.TestUtils;
using FluentAssertions;
using Xunit.Abstractions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.IntegrationTests.Endpoints.Admin;

[Collection("UsesWireMockClient")]
public class DrainTests(ITestOutputHelper output) : AdminTestBase(output)
{
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
}
