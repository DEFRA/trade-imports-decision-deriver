using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Ipaffs.Constants;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions;

public class NoMatchDecisionsTest
{
    [Fact]
    public async Task WhenClearanceRequest_HasNotMatch_AndH220Checks_ThenNoDecisionShouldBeGeneratedWithReason()
    {
        // Arrange
        var cr = ClearanceRequestFixtures.ClearanceRequestFixture();
        foreach (var commodity in cr.Commodities!)
        {
            foreach (var commodityCheck in commodity.Checks!)
            {
                commodityCheck.CheckCode = "H220";
            }
            foreach (var document in commodity.Documents!)
            {
                document.DocumentCode = "N002";
            }
        }
        var matchingResult = new MatchingResult();
        matchingResult.AddDocumentNoMatch(
            "123",
            cr.Commodities[0].ItemNumber!.Value,
            cr.Commodities[0].Documents?[0].DocumentReference!.Value!,
            cr.Commodities[0].Documents?[0].DocumentCode
        );

        var matchingService = Substitute.For<IMatchingService>();
        matchingService
            .Process(Arg.Any<MatchingContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(matchingResult));

        var sut = new DecisionService(
            NullLogger<DecisionService>.Instance,
            matchingService,
            Array.Empty<IDecisionFinder>()
        );

        // Act
        var decisionResult = await sut.Process(
            new DecisionContext(new List<DecisionImportPreNotification>(), [new ClearanceRequestWrapper("123", cr)]),
            CancellationToken.None
        );

        // Assert
        decisionResult.Should().NotBeNull();
        decisionResult.Decisions.Count.Should().Be(9);
        decisionResult.Decisions[0].DecisionCode.Should().Be(DecisionCode.X00);
        decisionResult
            .Decisions[0]
            .DecisionReason.Should()
            .Be(
                "This customs declaration with a GMS product has been selected for HMI inspection. Either create a new CHED PP or amend an existing one referencing the GMS product. Amend the customs declaration to reference the CHED PP."
            );

        await Task.CompletedTask;
    }

    [Fact]
    public async Task WhenClearanceRequest_HasNotMatch_AndH224Checks_ThenNoDecisionShouldBeGeneratedWithReason()
    {
        // Arrange
        var cr = ClearanceRequestFixtures.ClearanceRequestFixture();
        foreach (var commodity in cr.Commodities!)
        {
            foreach (var commodityCheck in commodity.Checks!)
            {
                commodityCheck.CheckCode = "H224";
            }
            foreach (var document in commodity.Documents!)
            {
                document.DocumentCode = "9115";
            }
        }
        var matchingResult = new MatchingResult();
        matchingResult.AddDocumentNoMatch(
            "123",
            cr.Commodities[0].ItemNumber!.Value,
            cr.Commodities[0].Documents?[0].DocumentReference!.Value!,
            cr.Commodities[0].Documents?[0].DocumentCode!
        );

        var matchingService = Substitute.For<IMatchingService>();
        matchingService
            .Process(Arg.Any<MatchingContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(matchingResult));

        var sut = new DecisionService(
            NullLogger<DecisionService>.Instance,
            matchingService,
            Array.Empty<IDecisionFinder>()
        );

        // Act
        var decisionResult = await sut.Process(
            new DecisionContext(new List<DecisionImportPreNotification>(), [new ClearanceRequestWrapper("123", cr)]),
            CancellationToken.None
        );

        // Assert
        decisionResult.Should().NotBeNull();
        decisionResult.Decisions.Count.Should().Be(9);
        decisionResult.Decisions[0].DecisionCode.Should().Be(DecisionCode.X00);
        decisionResult
            .Decisions[0]
            .DecisionReason.Should()
            .Be(
                "Customs declaration clearance withheld. Awaiting IUU check outcome. Contact Port Health Authority (imports) or Marine Management Organisation (landings)."
            );

        await Task.CompletedTask;
    }

    [Fact]
    public async Task WhenClearanceRequest_HasNotMatch_AndChecks_ThenNoDecisionShouldBeGeneratedWithReason()
    {
        // Arrange
        var cr = ClearanceRequestFixtures.ClearanceRequestFixture();
        foreach (var commodity in cr.Commodities!)
        {
            foreach (var commodityCheck in commodity.Checks!)
            {
                commodityCheck.CheckCode = "H219";
            }
            foreach (var document in commodity.Documents!)
            {
                document.DocumentCode = "9115";
            }
        }
        var matchingResult = new MatchingResult();
        matchingResult.AddDocumentNoMatch(
            "123",
            cr.Commodities[0].ItemNumber!.Value,
            cr.Commodities[0].Documents?[0].DocumentReference!.Value!,
            cr.Commodities[0].Documents?[0].DocumentCode
        );

        var matchingService = Substitute.For<IMatchingService>();
        matchingService
            .Process(Arg.Any<MatchingContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(matchingResult));

        var sut = new DecisionService(
            NullLogger<DecisionService>.Instance,
            matchingService,
            Array.Empty<IDecisionFinder>()
        );

        // Act
        var decisionResult = await sut.Process(
            new DecisionContext(new List<DecisionImportPreNotification>(), [new ClearanceRequestWrapper("123", cr)]),
            CancellationToken.None
        );

        // Assert
        decisionResult.Should().NotBeNull();
        decisionResult.Decisions.Count.Should().Be(9);
        decisionResult.Decisions[0].DecisionCode.Should().Be(DecisionCode.X00);
        decisionResult
            .Decisions[0]
            .DecisionReason.Should()
            .Be(
                "This IPAFFS pre-notification reference cannot be found in IPAFFS. Please check that the reference is correct."
            );

        await Task.CompletedTask;
    }

    [Fact]
    public async Task WhenClearanceRequest_HasNotMatch_AndNoChecks_ThenNoDecisionShouldBeGenerated()
    {
        // Arrange
        var matchingService = Substitute.For<IMatchingService>();
        var cr = ClearanceRequestFixtures.ClearanceRequestFixture();
        foreach (var commodity in cr.Commodities!)
        {
            commodity.Checks = [];
        }

        var sut = new DecisionService(
            NullLogger<DecisionService>.Instance,
            matchingService,
            Array.Empty<IDecisionFinder>()
        );

        var matchingResult = new MatchingResult();
        matchingResult.AddDocumentNoMatch(
            "123",
            cr.Commodities[0].ItemNumber!.Value,
            cr.Commodities[0].Documents?[0].DocumentReference!.Value!,
            cr.Commodities[0].Documents?[0].DocumentCode!
        );
        matchingService
            .Process(Arg.Any<MatchingContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(matchingResult));

        // Act
        var decisionResult = await sut.Process(
            new DecisionContext(new List<DecisionImportPreNotification>(), [new ClearanceRequestWrapper("123", cr)]),
            CancellationToken.None
        );

        // Assert
        decisionResult.Should().NotBeNull();
        decisionResult.Decisions.Count.Should().Be(0);

        await Task.CompletedTask;
    }

    [Fact]
    public async Task WhenClearanceRequest_HasNotMatch_ThenDecisionCodeShouldBeNoMatch()
    {
        // Arrange
        var matchingService = Substitute.For<IMatchingService>();
        var cr = ClearanceRequestFixtures.ClearanceRequestFixture();
        cr.Commodities = cr.Commodities!.Take(1).ToArray();
        cr.Commodities[0].Checks = [new CommodityCheck { CheckCode = "TEST" }];

        var sut = new DecisionService(
            NullLogger<DecisionService>.Instance,
            matchingService,
            Array.Empty<IDecisionFinder>()
        );

        var matchingResult = new MatchingResult();
        matchingResult.AddDocumentNoMatch(
            "123",
            cr.Commodities[0].ItemNumber!.Value,
            cr.Commodities[0].Documents?[0].DocumentReference!.Value!,
            cr.Commodities[0].Documents?[0].DocumentCode!
        );
        matchingService
            .Process(Arg.Any<MatchingContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(matchingResult));

        // Act
        var decisionResult = await sut.Process(
            new DecisionContext(new List<DecisionImportPreNotification>(), [new ClearanceRequestWrapper("123", cr)]),
            CancellationToken.None
        );

        // Assert
        decisionResult.Should().NotBeNull();
        decisionResult.Decisions.Count.Should().Be(1);
        decisionResult.Decisions[0].DecisionCode.Should().Be(DecisionCode.X00);

        await Task.CompletedTask;
    }

    [Fact]
    public async Task When_processing_chedpp_phsi_hmi_with_no_notification()
    {
        var decisionContext = new DecisionContext(
            [],
            [
                new ClearanceRequestWrapper(
                    "25GB99999999999021",
                    new ClearanceRequest
                    {
                        Commodities =
                        [
                            new Commodity
                            {
                                ItemNumber = 1,
                                Documents =
                                [
                                    new ImportDocument()
                                    {
                                        DocumentCode = "N851",
                                        DocumentReference = new ImportDocumentReference("GBCHD2025.9200009"),
                                        DocumentStatus = "JE",
                                        DocumentControl = "P",
                                    },
                                    new ImportDocument()
                                    {
                                        DocumentCode = "N002",
                                        DocumentReference = new ImportDocumentReference("GBCHD2025.9200009"),
                                        DocumentStatus = "JE",
                                        DocumentControl = "P",
                                    },
                                ],
                                Checks =
                                [
                                    new CommodityCheck { CheckCode = "H219", DepartmentCode = "PHSI" },
                                    new CommodityCheck { CheckCode = "H218", DepartmentCode = "HMI" },
                                ],
                            },
                        ],
                    }
                ),
            ]
        );

        var sut = new DecisionService(
            NullLogger<DecisionService>.Instance,
            new MatchingService(),
            [
                new ChedADecisionFinder(),
                new ChedDDecisionFinder(),
                new ChedPDecisionFinder(),
                new ChedPPDecisionFinder(),
                new IuuDecisionFinder(),
            ]
        );

        var decisionResult = await sut.Process(decisionContext, CancellationToken.None);

        decisionResult.Decisions.Count.Should().Be(2);
        decisionResult.Decisions[0].DecisionCode.Should().Be(DecisionCode.X00);
        decisionResult.Decisions[1].DecisionCode.Should().Be(DecisionCode.X00);
    }
}
