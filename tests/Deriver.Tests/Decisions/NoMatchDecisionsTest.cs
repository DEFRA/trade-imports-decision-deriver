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
        decisionResult.Decisions.Count.Should().Be(5);
        decisionResult.Decisions[0].DecisionCode.Should().Be(DecisionCode.X00);
        decisionResult.Decisions[0].DecisionReason.Should().Be(DocumentDecisionReasons.GmsInspection);

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
        decisionResult.Decisions.Count.Should().Be(3);
        decisionResult.Decisions[0].DecisionCode.Should().Be(DecisionCode.X00);
        decisionResult.Decisions[0].DecisionReason.Should().Be(DocumentDecisionReasons.OrphanCheckCode);

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
                document.DocumentReference = new ImportDocumentReference("Test.1234567");
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
        decisionResult.Decisions.Count.Should().Be(5);
        decisionResult.Decisions[0].DecisionCode.Should().Be(DecisionCode.X00);
        decisionResult
            .Decisions[0]
            .DecisionReason.Should()
            .Be("CHED reference Test.1234567 cannot be found in IPAFFS. Check that the reference is correct.");

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

    [Fact]
    public async Task When_processing_chedpp_with_new_c085_with_no_notification()
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
                                        DocumentCode = "C085",
                                        DocumentReference = new ImportDocumentReference("GBCHD2025.9200009"),
                                        DocumentStatus = "JE",
                                        DocumentControl = "P",
                                    },
                                ],
                                Checks = [new CommodityCheck { CheckCode = "H219", DepartmentCode = "PHSI" }],
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

        decisionResult.Decisions.Count.Should().Be(1);
        decisionResult.Decisions[0].DecisionCode.Should().Be(DecisionCode.X00);
        decisionResult
            .Decisions[0]
            .DecisionReason.Should()
            .Be("CHED reference GBCHD2025.9200009 cannot be found in IPAFFS. Check that the reference is correct.");
    }

    [Fact]
    public async Task When_processing_chedpp_with_phsi_and_all_three_document_codes_Then_should_return_expected_decisions()
    {
        // Arrange
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
                                        DocumentCode = "C085",
                                        DocumentReference = new ImportDocumentReference("GBCHD2025.9200019"),
                                        DocumentStatus = "JE",
                                        DocumentControl = "P",
                                    },
                                    new ImportDocument()
                                    {
                                        DocumentCode = "9115",
                                        DocumentReference = new ImportDocumentReference("GBCHD2025.9200029"),
                                        DocumentStatus = "JE",
                                        DocumentControl = "P",
                                    },
                                ],
                                Checks = [new CommodityCheck { CheckCode = "H219", DepartmentCode = "PHSI" }],
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

        // Act
        var decisionResult = await sut.Process(decisionContext, CancellationToken.None);

        // Assert

        decisionResult.Should().NotBeNull();
        decisionResult.Decisions.Count.Should().Be(3);
        decisionResult.Decisions[0].CheckCode.Should().Be("H219");
        decisionResult.Decisions[0].DecisionCode.Should().Be(DecisionCode.X00);
        decisionResult.Decisions[0].DocumentCode.Should().Be("N851");

        decisionResult.Decisions[1].CheckCode.Should().Be("H219");
        decisionResult.Decisions[1].DecisionCode.Should().Be(DecisionCode.X00);
        decisionResult.Decisions[1].DocumentCode.Should().Be("C085");

        decisionResult.Decisions[2].CheckCode.Should().Be("H219");
        decisionResult.Decisions[2].DecisionCode.Should().Be(DecisionCode.X00);
        decisionResult.Decisions[2].DocumentCode.Should().Be("9115");
    }

    [Fact]
    public async Task When_processing_chedpp_with_phsi_and_all_three_document_codes_Then_should_return_expected_decisions1()
    {
        // Arrange
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

                                Checks = [new CommodityCheck { CheckCode = "H220", DepartmentCode = "HMI" }],
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

        // Act
        var decisionResult = await sut.Process(decisionContext, CancellationToken.None);

        // Assert

        decisionResult.Should().NotBeNull();
        decisionResult.Decisions.Count.Should().Be(1);
        decisionResult.Decisions[0].CheckCode.Should().Be("H220");
        decisionResult.Decisions[0].DecisionCode.Should().Be(DecisionCode.X00);
        decisionResult.Decisions[0].DocumentCode.Should().Be(null);
        decisionResult.Decisions[0].DocumentReference.Should().Be(String.Empty);
        decisionResult.Decisions[0].DecisionReason.Should().Be(DocumentDecisionReasons.GmsInspection);
    }

    [Fact]
    public async Task When_processing_orphan_check_code_Then_should_return_expected_decisions()
    {
        // Arrange
        var decisionContext = new DecisionContext(
            [
                new DecisionImportPreNotification()
                {
                    Id = "CHEDPP.GB.2025.9200009",
                    Status = ImportNotificationStatus.Validated,
                    ImportNotificationType = ImportNotificationType.Cvedp,
                    UpdatedSource = DateTime.UtcNow,
                    ConsignmentDecision = ConsignmentDecision.AcceptableForInternalMarket,
                    NotAcceptableAction = null,
                    IuuCheckRequired = null,
                    IuuOption = null,
                    NotAcceptableReasons = null,
                    HasPartTwo = true,
                },
            ],
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
                                        DocumentCode = "N853",
                                        DocumentReference = new ImportDocumentReference("GBCHD2025.9200009"),
                                        DocumentStatus = "JE",
                                        DocumentControl = "P",
                                    },
                                    new ImportDocument()
                                    {
                                        DocumentCode = "C678",
                                        DocumentReference = new ImportDocumentReference("GBCHD2025.9200009"),
                                        DocumentStatus = "JE",
                                        DocumentControl = "P",
                                    },
                                ],
                                Checks =
                                [
                                    new CommodityCheck { CheckCode = "H222", DepartmentCode = "PHA" },
                                    new CommodityCheck { CheckCode = "H223", DepartmentCode = "PHA" },
                                    new CommodityCheck { CheckCode = "H219", DepartmentCode = "PHA" },
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

        // Act
        var decisionResult = await sut.Process(decisionContext, CancellationToken.None);

        // Assert

        decisionResult.Should().NotBeNull();
        decisionResult.Decisions.Count.Should().Be(3);
        decisionResult.Decisions[0].CheckCode.Should().Be("H222");
        decisionResult.Decisions[0].DecisionCode.Should().Be(DecisionCode.C03);
        decisionResult.Decisions[0].DocumentCode.Should().Be("N853");

        decisionResult.Decisions[1].CheckCode.Should().Be("H223");
        decisionResult.Decisions[1].DecisionCode.Should().Be(DecisionCode.X00);
        decisionResult.Decisions[1].InternalDecisionCode.Should().Be(DecisionInternalFurtherDetail.E84);

        decisionResult.Decisions[2].CheckCode.Should().Be("H219");
        decisionResult.Decisions[2].DecisionCode.Should().Be(DecisionCode.X00);
        decisionResult.Decisions[2].InternalDecisionCode.Should().Be(DecisionInternalFurtherDetail.E83);
    }

    [Fact]
    public async Task When_processing_iuu_check_codes_Then_should_return_expected_decisions()
    {
        // Arrange
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
                                        DocumentCode = "N853",
                                        DocumentReference = new ImportDocumentReference("GBCHD2025.9200009"),
                                        DocumentStatus = "JE",
                                        DocumentControl = "P",
                                    },
                                    new ImportDocument()
                                    {
                                        DocumentCode = "C673",
                                        DocumentReference = new ImportDocumentReference("GBIUU-VARIOUS"),
                                        DocumentStatus = "JE",
                                        DocumentControl = "P",
                                    },
                                ],
                                Checks =
                                [
                                    new CommodityCheck { CheckCode = "H222", DepartmentCode = "PHA" },
                                    new CommodityCheck { CheckCode = "H224", DepartmentCode = "PHA" },
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

        // Act
        var decisionResult = await sut.Process(decisionContext, CancellationToken.None);

        // Assert

        decisionResult.Should().NotBeNull();
        decisionResult.Decisions.Count.Should().Be(2);
        decisionResult.Decisions[0].CheckCode.Should().Be("H222");
        decisionResult.Decisions[0].DecisionCode.Should().Be(DecisionCode.X00);
        decisionResult.Decisions[0].InternalDecisionCode.Should().Be(DecisionInternalFurtherDetail.E70);
        decisionResult.Decisions[0].DocumentCode.Should().Be("N853");

        decisionResult.Decisions[1].CheckCode.Should().Be("H224");
        decisionResult.Decisions[1].DecisionCode.Should().Be(DecisionCode.X00);
        decisionResult.Decisions[1].InternalDecisionCode.Should().Be(DecisionInternalFurtherDetail.E70);
        decisionResult.Decisions[1].DocumentCode.Should().Be("N853");
    }
}
