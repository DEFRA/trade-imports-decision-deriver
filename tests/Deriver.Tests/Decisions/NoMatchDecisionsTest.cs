using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
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
                "A Customs Declaration with a GMS product has been selected for HMI inspection. In IPAFFS create a CHEDPP and amend your licence to reference it. If a CHEDPP exists, amend your licence to reference it. Failure to do so will delay your Customs release"
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
}
