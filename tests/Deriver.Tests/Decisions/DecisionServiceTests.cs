using Btms.Business.Services.Decisions;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

// ReSharper disable InconsistentNaming

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions;

public class DecisionServiceTests
{
    [Theory]
    [InlineData(ImportNotificationType.Cveda, DecisionCode.C06, "H221")]
    public async Task When_processing_decisions_for_ched_type_notifications_not_requiring_iuu_check_Then_should_use_matching_ched_decision_finder_only(
        ImportNotificationType targetImportNotificationType,
        DecisionCode expectedDecisionCode,
        params string[] checkCode
    )
    {
        var matchingResult = new MatchingResult();
        matchingResult.AddMatch("notification-1", "clearancerequest-1", 1, "document-ref-1");

        var matchingService = Substitute.For<IMatchingService>();
        matchingService
            .Process(Arg.Any<MatchingContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(matchingResult));

        var decisionContext = CreateDecisionContext(targetImportNotificationType, checkCode, iuuCheckRequired: false);
        var chedAFinder = Substitute.For<IDecisionFinder>();
        chedAFinder.CanFindDecision(decisionContext.Notifications[0], Arg.Any<CheckCode>()).Returns(true);
        chedAFinder
            .FindDecision(decisionContext.Notifications[0], Arg.Any<CheckCode>())
            .Returns(new DecisionFinderResult(expectedDecisionCode, new CheckCode() { Value = checkCode[0] }));

        var sut = new DecisionService(
            NullLogger<DecisionService>.Instance,
            matchingService,
            [
                chedAFinder,
                new ChedDDecisionFinder(),
                new ChedPDecisionFinder(),
                new ChedPPDecisionFinder(),
                new IuuDecisionFinder(),
            ]
        );

        var decisionResult = await sut.Process(decisionContext, CancellationToken.None);

        decisionResult.Decisions.Should().HaveCount(1);
        decisionResult.Decisions[0].DecisionCode.Should().Be(expectedDecisionCode);
    }

    private static DecisionContext CreateDecisionContext(
        ImportNotificationType? importNotificationType,
        string[]? checkCodes,
        bool? iuuCheckRequired
    )
    {
        return new DecisionContext(
            [
                new ImportPreNotification
                {
                    ReferenceNumber = "notification-1",
                    ImportNotificationType = importNotificationType,
                    Version = 1,
                    UpdatedSource = DateTime.Now,
                    PartTwo = new PartTwo
                    {
                        ControlAuthority = new ControlAuthority { IuuCheckRequired = iuuCheckRequired },
                    },
                },
            ],
            [
                new ClearanceRequestWrapper(
                    "clearancerequest-1",
                    new ClearanceRequest()
                    {
                        Commodities =
                        [
                            new Commodity()
                            {
                                ItemNumber = 1,
                                Documents = [new ImportDocument() { DocumentCode = "9115" }],
                                Checks = checkCodes
                                    ?.Select(checkCode => new CommodityCheck() { CheckCode = checkCode })
                                    .ToArray(),
                            },
                        ],
                    }
                ),
            ]
        );
    }
}
