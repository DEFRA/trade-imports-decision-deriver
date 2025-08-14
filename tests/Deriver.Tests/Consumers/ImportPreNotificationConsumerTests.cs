using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Consumers;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SlimMessageBus.Host;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Consumers;

public class ImportPreNotificationConsumerTests
{
    [Fact]
    public async Task GivenACreatedEvent_AndCustomsDeclarationsNotExists_ThenDecisionShouldNotBeCreated()
    {
        // ARRANGE
        var apiClient = Substitute.For<ITradeImportsDataApiClient>();
        var decisionService = Substitute.For<IDecisionService>();
        var consumer = new ImportPreNotificationConsumer(
            NullLogger<ImportPreNotificationConsumer>.Instance,
            decisionService,
            apiClient,
            new TestCorrelationIdGenerator("CorrelationId")
        )
        {
            Context = new ConsumerContext(),
        };

        var createdEvent = ImportPreNotificationFixtures.ImportPreNotificationCreatedFixture();
        apiClient
            .GetCustomsDeclarationsByChedId(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(new CustomsDeclarationsResponse([]));

        var decisionResult = new DecisionResult();
        decisionResult.AddDecision("mrn", 1, "docref", "docCode", "checkCode", DecisionCode.C03);
        decisionService.Process(Arg.Any<DecisionContext>(), Arg.Any<CancellationToken>()).Returns(decisionResult);

        // ACT
        await consumer.OnHandle(createdEvent, CancellationToken.None);

        // ASSERT
        apiClient.ReceivedCalls().Count().Should().Be(1);
    }

    [Fact]
    public async Task GivenACreatedEvent_AndCustomsDeclarationsExist_AndVersionsAreTheSame_ThenDecisionShouldBeCreated()
    {
        // ARRANGE
        var apiClient = Substitute.For<ITradeImportsDataApiClient>();
        var decisionService = Substitute.For<IDecisionService>();
        var consumer = new ImportPreNotificationConsumer(
            NullLogger<ImportPreNotificationConsumer>.Instance,
            decisionService,
            apiClient,
            new TestCorrelationIdGenerator("CorrelationId")
        );

        var createdEvent = ImportPreNotificationFixtures.ImportPreNotificationCreatedFixture();

        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture();
        customsDeclaration = customsDeclaration with { Finalisation = null };
        apiClient
            .GetCustomsDeclarationsByChedId(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(new CustomsDeclarationsResponse([customsDeclaration]));

        apiClient
            .GetImportPreNotificationsByMrn(customsDeclaration.MovementReferenceNumber, Arg.Any<CancellationToken>())
            .Returns(
                new ImportPreNotificationsResponse(
                    [
                        new ImportPreNotificationResponse(
                            ImportPreNotificationFixtures.ImportPreNotificationFixture("test")!,
                            DateTime.Now,
                            DateTime.Now
                        ),
                    ]
                )
            );

        var decisionResult = new DecisionResult();
        decisionResult.AddDecision("mrn123", 1, "docref", "docCode", "checkCode", DecisionCode.C03);
        decisionService.Process(Arg.Any<DecisionContext>(), Arg.Any<CancellationToken>()).Returns(decisionResult);

        // ACT
        await consumer.OnHandle(createdEvent, CancellationToken.None);

        // ASSERT
        apiClient.ReceivedCalls().Count().Should().Be(4);
    }

    [Fact]
    public async Task GivenACreatedEvent_AndCustomsDeclarationsExist_ThenDecisionShouldBeCreated()
    {
        // ARRANGE
        var apiClient = Substitute.For<ITradeImportsDataApiClient>();
        var decisionService = Substitute.For<IDecisionService>();
        var consumer = new ImportPreNotificationConsumer(
            NullLogger<ImportPreNotificationConsumer>.Instance,
            decisionService,
            apiClient,
            new TestCorrelationIdGenerator("CorrelationId")
        );

        var createdEvent = ImportPreNotificationFixtures.ImportPreNotificationCreatedFixture();

        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture();
        customsDeclaration = customsDeclaration with { Finalisation = null };
        apiClient
            .GetCustomsDeclarationsByChedId(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(new CustomsDeclarationsResponse([customsDeclaration]));

        apiClient
            .GetImportPreNotificationsByMrn(customsDeclaration.MovementReferenceNumber, Arg.Any<CancellationToken>())
            .Returns(
                new ImportPreNotificationsResponse(
                    [
                        new ImportPreNotificationResponse(
                            ImportPreNotificationFixtures.ImportPreNotificationFixture(createdEvent.ResourceId)!,
                            DateTime.Now,
                            DateTime.Now
                        ),
                    ]
                )
            );

        var decisionResult = new DecisionResult();
        decisionResult.AddDecision("mrn123", 1, "docref", "docCode", "checkCode", DecisionCode.C03);
        decisionService.Process(Arg.Any<DecisionContext>(), Arg.Any<CancellationToken>()).Returns(decisionResult);

        // ACT
        await consumer.OnHandle(createdEvent, CancellationToken.None);

        // ASSERT
        apiClient.ReceivedCalls().Count().Should().Be(4);
    }

    [Fact]
    public async Task GivenACreatedEvent_AndCustomsDeclarationsExist_AndDecisionAlreadyExists_ThenDecisionShouldNotBeSent()
    {
        // ARRANGE
        var apiClient = Substitute.For<ITradeImportsDataApiClient>();
        var decisionService = Substitute.For<IDecisionService>();
        var consumer = new ImportPreNotificationConsumer(
            NullLogger<ImportPreNotificationConsumer>.Instance,
            decisionService,
            apiClient,
            new TestCorrelationIdGenerator("CorrelationId")
        );

        var createdEvent = ImportPreNotificationFixtures.ImportPreNotificationCreatedFixture();

        var notification = ImportPreNotificationFixtures.ImportPreNotificationFixture("test");

        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture();
        customsDeclaration.ClearanceRequest!.ExternalCorrelationId = "correlationId";
        customsDeclaration.ClearanceDecision!.CorrelationId = "correlationId";
        customsDeclaration.ClearanceRequest!.ExternalVersion = 22;
        customsDeclaration = customsDeclaration with
        {
            ClearanceDecision = new ClearanceDecision()
            {
                DecisionNumber = 4,
                ExternalVersionNumber = 22,
                Items =
                [
                    new ClearanceDecisionItem()
                    {
                        ItemNumber = 1,
                        Checks = [new ClearanceDecisionCheck() { CheckCode = "9115", DecisionCode = "C03" }],
                    },
                    new ClearanceDecisionItem()
                    {
                        ItemNumber = 2,
                        Checks = [new ClearanceDecisionCheck() { CheckCode = "9115", DecisionCode = "C03" }],
                    },
                    new ClearanceDecisionItem()
                    {
                        ItemNumber = 3,
                        Checks = [new ClearanceDecisionCheck() { CheckCode = "9115", DecisionCode = "C03" }],
                    },
                ],
            },
        };

        var results = new List<ClearanceDecisionResult>();
        var itemNumber = 1;
        foreach (var commodity in customsDeclaration.ClearanceRequest!.Commodities!)
        {
            foreach (var document in commodity.Documents!)
            {
                results.Add(
                    new ClearanceDecisionResult()
                    {
                        ItemNumber = itemNumber,
                        DocumentReference = document.DocumentReference!.Value,
                        CheckCode = "9115",
                        DecisionCode = "C03",
                    }
                );
            }

            itemNumber++;
        }

        customsDeclaration.ClearanceDecision.Results = results.ToArray();

        var decisionResult = new DecisionResult();
        for (var i = 0; i < (customsDeclaration.ClearanceRequest?.Commodities!).Length; i++)
        {
            var commodity = (customsDeclaration.ClearanceRequest?.Commodities!)[i];
            commodity.ItemNumber = i + 1;
            commodity.Checks = commodity.Checks!.Take(1).ToArray();
            commodity.Checks[0].CheckCode = "9115";
            foreach (var document in commodity.Documents!)
            {
                document.DocumentCode = "9115";
                decisionResult.AddDecision(
                    customsDeclaration.MovementReferenceNumber,
                    commodity.ItemNumber!.Value!,
                    document.DocumentReference!.Value,
                    document.DocumentCode,
                    commodity.Checks[0].CheckCode,
                    DecisionCode.C03
                );
            }
        }

        customsDeclaration = customsDeclaration with { Finalisation = null };

        apiClient
            .GetCustomsDeclaration(customsDeclaration.MovementReferenceNumber, Arg.Any<CancellationToken>())
            .Returns(customsDeclaration);

        apiClient
            .GetCustomsDeclarationsByChedId(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(new CustomsDeclarationsResponse([customsDeclaration]));

        apiClient
            .GetImportPreNotificationsByMrn(customsDeclaration.MovementReferenceNumber, Arg.Any<CancellationToken>())
            .Returns(
                new ImportPreNotificationsResponse(
                    [new ImportPreNotificationResponse(notification, DateTime.Now, DateTime.Now)]
                )
            );

        decisionService.Process(Arg.Any<DecisionContext>(), Arg.Any<CancellationToken>()).Returns(decisionResult);

        // ACT
        await consumer.OnHandle(createdEvent, CancellationToken.None);

        // ASSERT
        apiClient.ReceivedCalls().Count().Should().Be(3);
    }

    [Fact]
    public async Task GivenACreatedEvent_AndCustomsDeclarationsExist_AndAlreadyFinalised_ThenDecisionShouldNotBeSent()
    {
        // ARRANGE
        var apiClient = Substitute.For<ITradeImportsDataApiClient>();
        var decisionService = Substitute.For<IDecisionService>();
        var consumer = new ImportPreNotificationConsumer(
            NullLogger<ImportPreNotificationConsumer>.Instance,
            decisionService,
            apiClient,
            new TestCorrelationIdGenerator("CorrelationId")
        );

        var createdEvent = ImportPreNotificationFixtures.ImportPreNotificationCreatedFixture();

        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture();

        apiClient
            .GetCustomsDeclarationsByChedId(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(new CustomsDeclarationsResponse([customsDeclaration]));

        // ACT
        await consumer.OnHandle(createdEvent, CancellationToken.None);

        // ASSERT
        apiClient.ReceivedCalls().Count().Should().Be(1);
    }
}
