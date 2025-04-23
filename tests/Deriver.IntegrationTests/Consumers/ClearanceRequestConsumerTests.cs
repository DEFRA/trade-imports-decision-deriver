using System.Security.AccessControl;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using NSubstitute;
using Xunit.Abstractions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.IntegrationTests.Consumers;

[Collection("Non-Parallel Collection")]
public class ClearanceRequestConsumerTests(ITestOutputHelper outputHelper)
    : IntegrationTests(new DeriverWebApplicationFactory(), outputHelper)
{
    [Fact]
    public async Task EndToEndTest_ClearanceRequest()
    {
        ////_apiClient.ClearReceivedCalls();
        var @event = ClearanceRequestFixtures.ClearanceRequestCreatedFixture();

        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture();
        _apiClient.GetCustomsDeclaration(@event.ResourceId, Arg.Any<CancellationToken>()).Returns(customsDeclaration);

        _apiClient
            .GetImportPreNotificationsByMrn(customsDeclaration.MovementReferenceNumber, Arg.Any<CancellationToken>())
            .Returns(
                [
                    new ImportPreNotificationResponse(
                        ImportPreNotificationFixtures.ImportPreNotificationFixture("test")!,
                        DateTime.Now,
                        DateTime.Now
                    ),
                ]
            );

        await PurgeQueue();
        await SendMessage(@event, ResourceEventResourceTypes.CustomsDeclaration);

        await WaitOnQueueBeingProcessed();

        await _apiClient
            .Received(1)
            .PutCustomsDeclaration(
                Arg.Any<string>(),
                Arg.Any<CustomsDeclaration>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task EndToEndTest_ImportPreNotification()
    {
        ////_apiClient.ClearReceivedCalls();
        var importNotification = ImportPreNotificationFixtures.ImportPreNotificationCreatedFixture();

        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture();
        _apiClient
            .GetCustomsDeclarationsByChedId(importNotification.ResourceId, Arg.Any<CancellationToken>())
            .Returns([customsDeclaration]);

        _apiClient
            .GetImportPreNotificationsByMrn(customsDeclaration.MovementReferenceNumber, Arg.Any<CancellationToken>())
            .Returns(
                [
                    new ImportPreNotificationResponse(
                        ImportPreNotificationFixtures.ImportPreNotificationFixture("test")!,
                        DateTime.Now,
                        DateTime.Now
                    ),
                ]
            );

        _apiClient
            .GetCustomsDeclaration(customsDeclaration.MovementReferenceNumber, Arg.Any<CancellationToken>())
            .Returns(customsDeclaration);

        await PurgeQueue();
        await SendMessage(importNotification, ResourceEventResourceTypes.ImportPreNotification);

        await WaitOnQueueBeingProcessed();

        await _apiClient
            .Received(1)
            .PutCustomsDeclaration(
                Arg.Any<string>(),
                Arg.Any<CustomsDeclaration>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            );
    }
}
