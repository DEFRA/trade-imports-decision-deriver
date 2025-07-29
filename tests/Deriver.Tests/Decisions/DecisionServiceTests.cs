using System.Linq;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Ipaffs.Constants;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit.Abstractions;

// ReSharper disable InconsistentNaming

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions;

public class DecisionServiceTests
{
    private readonly ITestOutputHelper output;

    public DecisionServiceTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Theory]
    [InlineData(ImportNotificationType.Cveda, DecisionCode.C06, "H221")]
    public async Task When_processing_decisions_for_ched_type_notifications_not_requiring_iuu_check_Then_should_use_matching_ched_decision_finder_only(
        string targetImportNotificationType,
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
            .Returns(new DecisionFinderResult(expectedDecisionCode, new CheckCode { Value = checkCode[0] }));

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

    [Fact]
    public async Task When_processing_clearance_request_with_null_documents_then_no_match_should_be_generated()
    {
        var matchingResult = new MatchingResult();

        var matchingService = Substitute.For<IMatchingService>();
        matchingService
            .Process(Arg.Any<MatchingContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(matchingResult));

        var decisionContext = new DecisionContext(
            [],
            [
                new ClearanceRequestWrapper(
                    "clearancerequest-1",
                    new ClearanceRequest
                    {
                        Commodities =
                        [
                            new Commodity
                            {
                                ItemNumber = 1,
                                Documents = null,
                                Checks = [new CommodityCheck { CheckCode = "H221" }],
                            },
                        ],
                    }
                ),
            ]
        );

        var sut = new DecisionService(
            NullLogger<DecisionService>.Instance,
            matchingService,
            [
                new ChedADecisionFinder(),
                new ChedDDecisionFinder(),
                new ChedPDecisionFinder(),
                new ChedPPDecisionFinder(),
                new IuuDecisionFinder(),
            ]
        );

        var decisionResult = await sut.Process(decisionContext, CancellationToken.None);

        decisionResult.Decisions.Should().HaveCount(1);
        decisionResult.Decisions[0].DecisionCode.Should().Be(DecisionCode.X00);
        decisionResult.Decisions[0].InternalDecisionCode.Should().Be(DecisionInternalFurtherDetail.E87);
    }

    [Fact]
    public async Task When_processing_clearance_request_with_empty_documents_then_no_match_should_be_generated()
    {
        var matchingResult = new MatchingResult();

        var matchingService = Substitute.For<IMatchingService>();
        matchingService
            .Process(Arg.Any<MatchingContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(matchingResult));

        var decisionContext = new DecisionContext(
            [],
            [
                new ClearanceRequestWrapper(
                    "clearancerequest-1",
                    new ClearanceRequest
                    {
                        Commodities =
                        [
                            new Commodity
                            {
                                ItemNumber = 1,
                                Documents = [],
                                Checks = [new CommodityCheck { CheckCode = "H221" }],
                            },
                        ],
                    }
                ),
            ]
        );

        var sut = new DecisionService(
            NullLogger<DecisionService>.Instance,
            matchingService,
            [
                new ChedADecisionFinder(),
                new ChedDDecisionFinder(),
                new ChedPDecisionFinder(),
                new ChedPPDecisionFinder(),
                new IuuDecisionFinder(),
            ]
        );

        var decisionResult = await sut.Process(decisionContext, CancellationToken.None);

        decisionResult.Decisions.Should().HaveCount(1);
        decisionResult.Decisions[0].DecisionCode.Should().Be(DecisionCode.X00);
        decisionResult.Decisions[0].InternalDecisionCode.Should().Be(DecisionInternalFurtherDetail.E87);
    }

    [Fact]
    public async Task When_processing_chedpp_with_phsi_and_missing_hmi()
    {
        var matchingResult = new MatchingResult();

        var matchingService = Substitute.For<IMatchingService>();
        matchingService
            .Process(Arg.Any<MatchingContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(matchingResult));

        var decisionContext = new DecisionContext(
            [
                new DecisionImportPreNotification()
                {
                    Id = "CHEDPP.GB.2025.6248785",
                    Status = ImportNotificationStatus.Validated,
                    ImportNotificationType = ImportNotificationType.Chedpp,
                    UpdatedSource = DateTime.UtcNow,
                    ConsignmentDecision = null,
                    NotAcceptableAction = null,
                    IuuCheckRequired = null,
                    IuuOption = null,
                    NotAcceptableReasons = null,
                    InspectionRequired = "Required",
                    Commodities =
                    [
                        new DecisionCommodityComplement() { HmiDecision = "NOTREQUIRED", PhsiDecision = "REQUIRED" },
                    ],
                    CommodityChecks =
                    [
                        new DecisionCommodityCheck.Check() { Type = "PHSI_DOCUMENT", Status = "Compliant" },
                        new DecisionCommodityCheck.Check() { Type = "PHSI_IDENTITY", Status = "Auto cleared" },
                        new DecisionCommodityCheck.Check() { Type = "PHSI_PHYSICAL", Status = "Auto cleared" },
                    ],
                },
            ],
            [
                new ClearanceRequestWrapper(
                    "25GB7FOTHLNCYKEAR2",
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
                                        DocumentCode = "N002",
                                        DocumentReference = new ImportDocumentReference("GBCHD2025.6248785"),
                                        DocumentStatus = "AE",
                                        DocumentControl = "P",
                                    },
                                    new ImportDocument()
                                    {
                                        DocumentCode = "N851",
                                        DocumentReference = new ImportDocumentReference("GBCHD2025.6248785"),
                                        DocumentStatus = "AE",
                                        DocumentControl = "P",
                                    },
                                ],
                                Checks =
                                [
                                    new CommodityCheck { CheckCode = "H218", DepartmentCode = "HMI" },
                                    new CommodityCheck { CheckCode = "H219", DepartmentCode = "PHSI" },
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

        decisionResult.Decisions.Max(x => x.DecisionCode).Should().Be(DecisionCode.H01);
    }

    [Fact]
    public async Task When_processing_chedpp_phsi_with_both_validated_and_rejected_documents()
    {
        var decisionContext = new DecisionContext(
            [
                new DecisionImportPreNotification()
                {
                    Id = "CHEDPP.GB.2025.9200009V",
                    Status = ImportNotificationStatus.Validated,
                    ImportNotificationType = ImportNotificationType.Chedpp,
                    UpdatedSource = DateTime.UtcNow,
                    ConsignmentDecision = null,
                    NotAcceptableAction = null,
                    IuuCheckRequired = null,
                    IuuOption = null,
                    NotAcceptableReasons = null,
                    CommodityChecks =
                    [
                        new DecisionCommodityCheck.Check() { Type = "PHSI_DOCUMENT", Status = "Compliant" },
                        new DecisionCommodityCheck.Check() { Type = "PHSI_IDENTITY", Status = "Auto cleared" },
                        new DecisionCommodityCheck.Check() { Type = "PHSI_PHYSICAL", Status = "Auto cleared" },
                    ],
                },
                new DecisionImportPreNotification()
                {
                    Id = "CHEDPP.GB.2025.9200009R",
                    Status = ImportNotificationStatus.Rejected,
                    ImportNotificationType = ImportNotificationType.Chedpp,
                    UpdatedSource = DateTime.UtcNow,
                    ConsignmentDecision = null,
                    NotAcceptableAction = null,
                    IuuCheckRequired = null,
                    IuuOption = null,
                    NotAcceptableReasons = null,
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
                                        DocumentCode = "N851",
                                        DocumentReference = new ImportDocumentReference("GBCHD2025.9200009R"),
                                        DocumentStatus = "JE",
                                        DocumentControl = "P",
                                    },
                                ],
                                Checks = [new CommodityCheck { CheckCode = "H219", DepartmentCode = "PHSI" }],
                            },
                            new Commodity
                            {
                                ItemNumber = 2,
                                Documents =
                                [
                                    new ImportDocument()
                                    {
                                        DocumentCode = "N851",
                                        DocumentReference = new ImportDocumentReference("GBCHD2025.9200009V"),
                                        DocumentStatus = "JE",
                                        DocumentControl = "P",
                                    },
                                ],
                                Checks = [new CommodityCheck { CheckCode = "H219", DepartmentCode = "PHSI" }],
                            },
                            new Commodity
                            {
                                ItemNumber = 3,
                                Documents =
                                [
                                    new ImportDocument()
                                    {
                                        DocumentCode = "N851",
                                        DocumentReference = new ImportDocumentReference("GBCHD2025.9200009V"),
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

        decisionResult.Decisions.Count.Should().Be(3);
        decisionResult.Decisions[0].DecisionCode.Should().Be(DecisionCode.N02);
        decisionResult.Decisions[1].DecisionCode.Should().Be(DecisionCode.C03);
        decisionResult.Decisions[2].DecisionCode.Should().Be(DecisionCode.C03);
    }

    [Theory]
    [InlineData("both", "Compliant", "Compliant", "Compliant", "Compliant", DecisionCode.C03, DecisionCode.C03)]
    [InlineData(
        "both",
        "Non compliant",
        "Non compliant",
        "Non compliant",
        "Compliant",
        DecisionCode.N01,
        DecisionCode.C03
    )]
    [InlineData("both", "Compliant", "Compliant", "Compliant", "Non compliant", DecisionCode.C03, DecisionCode.N01)]
    [InlineData(
        "both",
        "Non compliant",
        "Non compliant",
        "Non compliant",
        "Non compliant",
        DecisionCode.N01,
        DecisionCode.N01
    )]
    [InlineData("both", "Compliant", "Compliant", "Compliant", "Missing", DecisionCode.C03, DecisionCode.H01)]
    [InlineData(
        "both",
        "Non compliant",
        "Non compliant",
        "Non compliant",
        "Missing",
        DecisionCode.N01,
        DecisionCode.H01
    )]
    [InlineData("both", "Missing", "Missing", "Missing", "Compliant", DecisionCode.H01, DecisionCode.C03)]
    [InlineData("both", "Missing", "Missing", "Missing", "Non compliant", DecisionCode.H01, DecisionCode.N01)]
    [InlineData("phsi", "Compliant", "Compliant", "Compliant", "Missing", DecisionCode.C03)]
    [InlineData("phsi", "Non compliant", "Non compliant", "Non compliant", "Missing", DecisionCode.N01)]
    [InlineData("phsi", "Compliant", "Compliant", "Compliant", "Compliant", DecisionCode.C03)]
    [InlineData("phsi", "Compliant", "Compliant", "Compliant", "Non compliant", DecisionCode.C03)]
    [InlineData("phsi", "Non compliant", "Non compliant", "Non compliant", "Non compliant", DecisionCode.N01)]
    [InlineData("phsi", "Non compliant", "Non compliant", "Non compliant", "Compliant", DecisionCode.N01)]
    [InlineData("hmi", "Compliant", "Compliant", "Compliant", "Compliant", DecisionCode.C03)]
    [InlineData("hmi", "Non compliant", "Non compliant", "Non compliant", "Non compliant", DecisionCode.N01)]
    [InlineData("hmi", "Compliant", "Compliant", "Compliant", "Non compliant", DecisionCode.N01)]
    [InlineData("hmi", "Non compliant", "Non compliant", "Non compliant", "Compliant", DecisionCode.C03)]
    [InlineData("hmi", "Missing", "Missing", "Missing", "Compliant", DecisionCode.C03)]
    [InlineData("hmi", "Missing", "Missing", "Missing", "Non compliant", DecisionCode.N01)]
    // Mixed PHSI status scenarios
    [InlineData("both", "Compliant", "Non compliant", "Auto cleared", "Compliant", DecisionCode.N01, DecisionCode.C03)]
    [InlineData(
        "both",
        "Auto cleared",
        "Compliant",
        "Non compliant",
        "Non compliant",
        DecisionCode.N01,
        DecisionCode.N01
    )]
    [InlineData("phsi", "Compliant", "Non compliant", "Auto cleared", "Missing", DecisionCode.N01)]
    //  Edge case status scenarios
    [InlineData("both", "To do", "To do", "To do", "Compliant", DecisionCode.H01, DecisionCode.C03)]
    [InlineData("both", "Hold", "Hold", "Hold", "Non compliant", DecisionCode.H01, DecisionCode.N01)]
    [InlineData(
        "both",
        "To be inspected",
        "To be inspected",
        "To be inspected",
        "Compliant",
        DecisionCode.H02,
        DecisionCode.C03
    )]
    [InlineData(
        "both",
        "Not inspected",
        "Not inspected",
        "Not inspected",
        "Non compliant",
        DecisionCode.C02,
        DecisionCode.N01
    )]
    [InlineData("phsi", "To do", "To do", "To do", "Missing", DecisionCode.H01)]
    [InlineData("hmi", "Missing", "Missing", "Missing", "Hold", DecisionCode.H01)]
    [InlineData("hmi", "Missing", "Missing", "Missing", "To be inspected", DecisionCode.H02)]
    [InlineData("hmi", "Missing", "Missing", "Missing", "Not inspected", DecisionCode.C02)]
    public async Task When_processing_chedpp_scenarios_Then_should_return_expected_decisions(
        string scenario,
        string phsiDocumentStatus,
        string phsiIdentityStatus,
        string phsiPhysicalStatus,
        string hmiStatus,
        DecisionCode expectedPhsiDecisionCode,
        DecisionCode? expectedHmiDecisionCode = null
    )
    {
        // Arrange
        var matchingResult = new MatchingResult();
        matchingResult.AddMatch("CHEDPP.GB.2025.1234567", "25GB12345678901234", 1, "GBCHD2025.1234567");

        var matchingService = Substitute.For<IMatchingService>();
        matchingService
            .Process(Arg.Any<MatchingContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(matchingResult));

        var decisionContext = CreateChedppDecisionContext(
            scenario,
            phsiDocumentStatus,
            phsiIdentityStatus,
            phsiPhysicalStatus,
            hmiStatus
        );

        var sut = new DecisionService(
            NullLogger<DecisionService>.Instance,
            matchingService,
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
        decisionResult.Decisions.Should().NotBeEmpty();

        // Log  the scenario being tested for debugging
        output.WriteLine(
            $"Testing scenario: {scenario} - PHSI: {phsiDocumentStatus}/{phsiIdentityStatus}/{phsiPhysicalStatus}, HMI: {hmiStatus}"
        );

        // For scenarios with both PHSI and HMI checks, we need to verify the correct decision for each
        if (scenario == "both")
        {
            var phsiDecision = decisionResult.Decisions.FirstOrDefault(d => d.CheckCode == "H219");
            var hmiDecision = decisionResult.Decisions.FirstOrDefault(d => d.CheckCode == "H218");

            if (phsiDecision != null)
            {
                phsiDecision.DecisionCode.Should().Be(expectedPhsiDecisionCode);
            }

            if (hmiDecision != null && expectedHmiDecisionCode.HasValue)
            {
                hmiDecision.DecisionCode.Should().Be(expectedHmiDecisionCode.Value);
            }
        }
        else if (scenario == "phsi")
        {
            var phsiDecision = decisionResult.Decisions.FirstOrDefault(d => d.CheckCode == "H219");
            phsiDecision.Should().NotBeNull();
            phsiDecision!.DecisionCode.Should().Be(expectedPhsiDecisionCode);
        }
        else if (scenario == "hmi")
        {
            var hmiDecision = decisionResult.Decisions.FirstOrDefault(d => d.CheckCode == "H218");
            hmiDecision.Should().NotBeNull();
            hmiDecision!.DecisionCode.Should().Be(expectedPhsiDecisionCode);
        }
    }

    private static DecisionContext CreateDecisionContext(
        string? importNotificationType,
        string[]? checkCodes,
        bool? iuuCheckRequired
    )
    {
        return new DecisionContext(
            [
                new DecisionImportPreNotification
                {
                    Id = "notification-1",
                    ImportNotificationType = importNotificationType,
                    IuuCheckRequired = iuuCheckRequired,
                },
            ],
            [
                new ClearanceRequestWrapper(
                    "clearancerequest-1",
                    new ClearanceRequest
                    {
                        Commodities =
                        [
                            new Commodity
                            {
                                ItemNumber = 1,
                                Documents = [new ImportDocument { DocumentCode = "9115" }],
                                Checks = checkCodes
                                    ?.Select(checkCode => new CommodityCheck { CheckCode = checkCode })
                                    .ToArray(),
                            },
                        ],
                    }
                ),
            ]
        );
    }

    private static DecisionContext CreateChedppDecisionContext(
        string scenario,
        string phsiDocumentStatus,
        string phsiIdentityStatus,
        string phsiPhysicalStatus,
        string hmiStatus
    )
    {
        var commodityChecks = new List<DecisionCommodityCheck.Check>();

        // Add PHSI checks if scenario includes PHSI
        if ((scenario == "phsi" || scenario == "both") && phsiDocumentStatus != "Missing")
        {
            commodityChecks.Add(
                new DecisionCommodityCheck.Check { Type = "PHSI_DOCUMENT", Status = phsiDocumentStatus }
            );
        }
        if ((scenario == "phsi" || scenario == "both") && phsiIdentityStatus != "Missing")
        {
            commodityChecks.Add(
                new DecisionCommodityCheck.Check { Type = "PHSI_IDENTITY", Status = phsiIdentityStatus }
            );
        }
        if ((scenario == "phsi" || scenario == "both") && phsiPhysicalStatus != "Missing")
        {
            commodityChecks.Add(
                new DecisionCommodityCheck.Check { Type = "PHSI_PHYSICAL", Status = phsiPhysicalStatus }
            );
        }

        // Add HMI check if scenario includes HMI
        if ((scenario == "hmi" || scenario == "both") && hmiStatus != "Missing")
        {
            commodityChecks.Add(new DecisionCommodityCheck.Check { Type = "HMI", Status = hmiStatus });
        }

        var clearanceRequestChecks = new List<CommodityCheck>();

        if (scenario == "phsi" || scenario == "both")
        {
            clearanceRequestChecks.Add(new CommodityCheck { CheckCode = "H219", DepartmentCode = "PHSI" });
        }

        if (scenario == "hmi" || scenario == "both")
        {
            clearanceRequestChecks.Add(new CommodityCheck { CheckCode = "H218", DepartmentCode = "HMI" });
        }

        return new DecisionContext(
            [
                new DecisionImportPreNotification()
                {
                    Id = "CHEDPP.GB.2025.1234567",
                    Status = ImportNotificationStatus.Validated,
                    ImportNotificationType = ImportNotificationType.Chedpp,
                    UpdatedSource = DateTime.UtcNow,
                    ConsignmentDecision = null,
                    NotAcceptableAction = null,
                    IuuCheckRequired = null,
                    IuuOption = null,
                    NotAcceptableReasons = null,
                    InspectionRequired = "Required",
                    Commodities =
                    [
                        new DecisionCommodityComplement()
                        {
                            HmiDecision = scenario == "hmi" || scenario == "both" ? "REQUIRED" : "NOTREQUIRED",
                            PhsiDecision = scenario == "phsi" || scenario == "both" ? "REQUIRED" : "NOTREQUIRED",
                        },
                    ],
                    CommodityChecks = commodityChecks.ToArray(),
                },
            ],
            [
                new ClearanceRequestWrapper(
                    "25GB12345678901234",
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
                                        DocumentCode = "N002",
                                        DocumentReference = new ImportDocumentReference("GBCHD2025.1234567"),
                                        DocumentStatus = "AE",
                                        DocumentControl = "P",
                                    },
                                    new ImportDocument()
                                    {
                                        DocumentCode = "N851",
                                        DocumentReference = new ImportDocumentReference("GBCHD2025.1234567"),
                                        DocumentStatus = "AE",
                                        DocumentControl = "P",
                                    },
                                ],
                                Checks = clearanceRequestChecks.ToArray(),
                            },
                        ],
                    }
                ),
            ]
        );
    }
}
