using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Ipaffs.Constants;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.Processors;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using Xunit.Abstractions;
using ClearanceDecisionBuilder = Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.ClearanceDecisionBuilder;

// ReSharper disable InconsistentNaming

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions;

public class DecisionServiceTests(ITestOutputHelper output)
{
    [Fact]
    public void When_processing_clearance_request_with_null_documents_then_no_match_should_be_generated()
    {
        var decisionContext = new DecisionContextV2(
            [],
            [
                new CustomsDeclarationWrapper(
                    "clearancerequest-1",
                    new CustomsDeclaration()
                    {
                        ClearanceRequest = new ClearanceRequest
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
                        },
                    }
                ),
            ]
        );

        var decisionServiceV2 = new DecisionServiceV2(
            new ClearanceDecisionBuilder(new TestCorrelationIdGenerator("TEST")),
            new CheckProcessor(new TestDecisionRulesEngineFactory())
        );

        // Act
        var decisionResult = decisionServiceV2.Process(decisionContext);

        decisionResult.Should().HaveCount(1);
        decisionResult[0].Decision.Results?[0].DecisionCode.Should().Be(DecisionCode.X00.ToString());
        decisionResult[0]
            .Decision.Results?[0].InternalDecisionCode.Should()
            .Be(DecisionInternalFurtherDetail.E83.ToString());
        decisionResult[0].Decision.Results?[0].CheckCode.Should().Be("H221");
    }

    [Fact]
    public void When_processing_clearance_request_with_empty_documents_then_no_match_should_be_generated()
    {
        var decisionContext = new DecisionContextV2(
            [],
            [
                new CustomsDeclarationWrapper(
                    "clearancerequest-1",
                    new CustomsDeclaration()
                    {
                        ClearanceRequest = new ClearanceRequest
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
                        },
                    }
                ),
            ]
        );

        var decisionServiceV2 = new DecisionServiceV2(
            new ClearanceDecisionBuilder(new TestCorrelationIdGenerator("TEST")),
            new CheckProcessor(new TestDecisionRulesEngineFactory())
        );

        // Act
        var decisionResult = decisionServiceV2.Process(decisionContext);

        decisionResult.Should().HaveCount(1);
        decisionResult[0].Decision.Results?[0].DecisionCode.Should().Be(DecisionCode.X00.ToString());
        decisionResult[0]
            .Decision.Results?[0].InternalDecisionCode.Should()
            .Be(DecisionInternalFurtherDetail.E83.ToString());
    }

    [Fact]
    public void When_processing_chedpp_with_phsi_and_missing_hmi()
    {
        var decisionContext = new DecisionContextV2(
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
                    HasPartTwo = true,
                },
            ],
            [
                new CustomsDeclarationWrapper(
                    "25GB7FOTHLNCYKEAR2",
                    new CustomsDeclaration()
                    {
                        ClearanceRequest = new ClearanceRequest
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
                        },
                    }
                ),
            ]
        );

        var decisionServiceV2 = new DecisionServiceV2(
            new ClearanceDecisionBuilder(new TestCorrelationIdGenerator("TEST")),
            new CheckProcessor(new TestDecisionRulesEngineFactory())
        );

        // Act
        var decisionResult = decisionServiceV2.Process(decisionContext);

        decisionResult[0].Decision.Results!.Max(x => x.DecisionCode).Should().Be(DecisionCode.H01.ToString());
    }

    [Fact]
    public void When_processing_chedpp_phsi_with_both_validated_and_rejected_documents()
    {
        var decisionContext = new DecisionContextV2(
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
                    HasPartTwo = true,
                    Commodities =
                    [
                        new DecisionCommodityComplement()
                        {
                            Id = 1,
                            Weight = 56,
                            CommodityCode = "020711",
                        },
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
                    CommodityChecks =
                    [
                        new DecisionCommodityCheck.Check() { Type = "PHSI_DOCUMENT", Status = "Non compliant" },
                        new DecisionCommodityCheck.Check() { Type = "PHSI_IDENTITY", Status = "Auto cleared" },
                        new DecisionCommodityCheck.Check() { Type = "PHSI_PHYSICAL", Status = "Auto cleared" },
                    ],
                    HasPartTwo = true,
                },
            ],
            [
                new CustomsDeclarationWrapper(
                    "25GB99999999999021",
                    new CustomsDeclaration()
                    {
                        ClearanceRequest = new ClearanceRequest
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
                        },
                    }
                ),
            ]
        );

        var decisionServiceV2 = new DecisionServiceV2(
            new ClearanceDecisionBuilder(new TestCorrelationIdGenerator("TEST")),
            new CheckProcessor(new TestDecisionRulesEngineFactory())
        );

        // Act
        var decisionResult = decisionServiceV2.Process(decisionContext);

        decisionResult[0].Decision.Results?.Length.Should().Be(3);
        decisionResult[0].Decision.Results?[0].DecisionCode.Should().Be(DecisionCode.N01.ToString());
        decisionResult[0].Decision.Results?[1].DecisionCode.Should().Be(DecisionCode.C03.ToString());
        decisionResult[0].Decision.Results?[2].DecisionCode.Should().Be(DecisionCode.C03.ToString());
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
    public void When_processing_chedpp_scenarios_Then_should_return_expected_decisions(
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

        var decisionContext = CreateChedppDecisionContextV2(
            scenario,
            phsiDocumentStatus,
            phsiIdentityStatus,
            phsiPhysicalStatus,
            hmiStatus
        );

        var decisionServiceV2 = new DecisionServiceV2(
            new ClearanceDecisionBuilder(new TestCorrelationIdGenerator("TEST")),
            new CheckProcessor(new TestDecisionRulesEngineFactory())
        );

        // Act
        var decisionResult = decisionServiceV2.Process(decisionContext);

        // Assert
        decisionResult.Should().NotBeEmpty();

        // Log  the scenario being tested for debugging
        output.WriteLine(
            $"Testing scenario: {scenario} - PHSI: {phsiDocumentStatus}/{phsiIdentityStatus}/{phsiPhysicalStatus}, HMI: {hmiStatus}"
        );

        // For scenarios with both PHSI and HMI checks, we need to verify the correct decision for each
        if (scenario == "both")
        {
            var phsiDecision = decisionResult[0].Decision.Results!.FirstOrDefault(d => d.CheckCode == "H219");
            var hmiDecision = decisionResult[0].Decision.Results!.FirstOrDefault(d => d.CheckCode == "H218");

            if (phsiDecision != null)
            {
                phsiDecision.DecisionCode.Should().Be(expectedPhsiDecisionCode.ToString());
            }

            if (hmiDecision != null && expectedHmiDecisionCode.HasValue)
            {
                hmiDecision.DecisionCode.Should().Be(expectedHmiDecisionCode.Value.ToString());
            }
        }
        else if (scenario == "phsi")
        {
            var phsiDecision = decisionResult[0].Decision.Results!.FirstOrDefault(d => d.CheckCode == "H219");
            phsiDecision.Should().NotBeNull();
            phsiDecision!.DecisionCode.Should().Be(expectedPhsiDecisionCode.ToString());
        }
        else if (scenario == "hmi")
        {
            var hmiDecision = decisionResult[0].Decision.Results!.FirstOrDefault(d => d.CheckCode == "H218");
            hmiDecision.Should().NotBeNull();
            hmiDecision!.DecisionCode.Should().Be(expectedPhsiDecisionCode.ToString());
        }
    }

    [Fact]
    public void When_processing_chedpp_with_both_phsi_and_hmi_Then_should_return_expected_decisions()
    {
        // Arrange
        var decisionContext = new DecisionContextV2(
            [
                new DecisionImportPreNotification()
                {
                    Id = "CHEDPP.GB.2025.9200009",
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
                    HasPartTwo = true,
                },
            ],
            [
                new CustomsDeclarationWrapper(
                    "25GB99999999999021",
                    new CustomsDeclaration()
                    {
                        ClearanceRequest = new ClearanceRequest
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
                        },
                    }
                ),
            ]
        );

        var decisionServiceV2 = new DecisionServiceV2(
            new ClearanceDecisionBuilder(new TestCorrelationIdGenerator("TEST")),
            new CheckProcessor(new TestDecisionRulesEngineFactory())
        );

        // Act
        var decisionResult = decisionServiceV2.Process(decisionContext);

        // Assert

        decisionResult.Should().NotBeNull();
        decisionResult[0].Decision.Results?.Length.Should().Be(2);
        decisionResult[0].Decision.Results?[0].CheckCode.Should().Be("H219");
        decisionResult[0].Decision.Results?[0].DecisionCode.Should().Be(DecisionCode.C03.ToString());
        decisionResult[0].Decision.Results?[0].DocumentCode.Should().Be("N851");

        decisionResult[0].Decision.Results?[1].CheckCode.Should().Be("H218");
        decisionResult[0].Decision.Results?[1].DecisionCode.Should().Be(DecisionCode.H01.ToString());
        decisionResult[0].Decision.Results?[1].DocumentCode.Should().Be("N002");
    }

    [Fact]
    public void When_processing_chedpp_with_both_phsi_and_hmi_with_H220_Then_should_return_expected_decisions()
    {
        // Arrange
        var decisionContext = new DecisionContextV2(
            [
                new DecisionImportPreNotification()
                {
                    Id = "CHEDPP.GB.2025.9200009",
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
                        new DecisionCommodityCheck.Check() { Type = "PHSI_DOCUMENT", Status = "To do" },
                        new DecisionCommodityCheck.Check() { Type = "PHSI_IDENTITY", Status = "To do" },
                        new DecisionCommodityCheck.Check() { Type = "PHSI_PHYSICAL", Status = "To do" },
                        new DecisionCommodityCheck.Check() { Type = "HMI", Status = "Auto cleared" },
                    ],
                    HasPartTwo = true,
                },
            ],
            [
                new CustomsDeclarationWrapper(
                    "25GB99999999999021",
                    new CustomsDeclaration()
                    {
                        ClearanceRequest = new ClearanceRequest
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
                                        new CommodityCheck { CheckCode = "H220", DepartmentCode = "HMI" },
                                    ],
                                },
                            ],
                        },
                    }
                ),
            ]
        );

        var decisionServiceV2 = new DecisionServiceV2(
            new ClearanceDecisionBuilder(new TestCorrelationIdGenerator("TEST")),
            new CheckProcessor(new TestDecisionRulesEngineFactory())
        );

        // Act
        var decisionResult = decisionServiceV2.Process(decisionContext);

        // Assert

        decisionResult.Should().NotBeNull();
        decisionResult[0].Decision.Results?.Length.Should().Be(2);
        decisionResult[0].Decision.Results?[0].CheckCode.Should().Be("H219");
        decisionResult[0].Decision.Results?[0].DecisionCode.Should().Be(DecisionCode.H01.ToString());
        decisionResult[0].Decision.Results?[0].DocumentCode.Should().Be("N851");

        decisionResult[0].Decision.Results?[1].CheckCode.Should().Be("H220");
        decisionResult[0].Decision.Results?[1].DecisionCode.Should().Be(DecisionCode.C03.ToString());
        decisionResult[0].Decision.Results?[1].DocumentCode.Should().Be("N002");
    }

    [Fact]
    public void When_processing_chedpp_with_phsi_and_all_three_document_codes_Then_should_return_expected_decisions()
    {
        // Arrange
        var decisionContext = new DecisionContextV2(
            [
                new DecisionImportPreNotification()
                {
                    Id = "CHEDPP.GB.2025.9200009",
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
                        new DecisionCommodityCheck.Check() { Type = "PHSI_DOCUMENT", Status = "To do" },
                        new DecisionCommodityCheck.Check() { Type = "PHSI_IDENTITY", Status = "To do" },
                        new DecisionCommodityCheck.Check() { Type = "PHSI_PHYSICAL", Status = "To do" },
                        new DecisionCommodityCheck.Check() { Type = "HMI", Status = "Auto cleared" },
                    ],
                    HasPartTwo = true,
                },
            ],
            [
                new CustomsDeclarationWrapper(
                    "25GB99999999999021",
                    new CustomsDeclaration()
                    {
                        ClearanceRequest = new ClearanceRequest
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
                                            DocumentReference = new ImportDocumentReference("GBCHD2025.9200009"),
                                            DocumentStatus = "JE",
                                            DocumentControl = "P",
                                        },
                                        new ImportDocument()
                                        {
                                            DocumentCode = "9115",
                                            DocumentReference = new ImportDocumentReference("GBCHD2025.9200009"),
                                            DocumentStatus = "JE",
                                            DocumentControl = "P",
                                        },
                                    ],
                                    Checks = [new CommodityCheck { CheckCode = "H219", DepartmentCode = "PHSI" }],
                                },
                            ],
                        },
                    }
                ),
            ]
        );

        var decisionServiceV2 = new DecisionServiceV2(
            new ClearanceDecisionBuilder(new TestCorrelationIdGenerator("TEST")),
            new CheckProcessor(new TestDecisionRulesEngineFactory())
        );

        // Act
        var decisionResult = decisionServiceV2.Process(decisionContext);

        // Assert
        decisionResult.Should().NotBeNull();
        decisionResult[0].Decision.Results?.Length.Should().Be(3);
        decisionResult[0].Decision.Results?[0].CheckCode.Should().Be("H219");
        decisionResult[0].Decision.Results?[0].DecisionCode.Should().Be(nameof(DecisionCode.H01));
        decisionResult[0].Decision.Results?[0].DocumentCode.Should().Be("N851");

        decisionResult[0].Decision.Results?[1].CheckCode.Should().Be("H219");
        decisionResult[0].Decision.Results?[1].DecisionCode.Should().Be(nameof(DecisionCode.H01));
        decisionResult[0].Decision.Results?[1].DocumentCode.Should().Be("C085");

        decisionResult[0].Decision.Results?[2].CheckCode.Should().Be("H219");
        decisionResult[0].Decision.Results?[2].DecisionCode.Should().Be(nameof(DecisionCode.H01));
        decisionResult[0].Decision.Results?[2].DocumentCode.Should().Be("9115");
    }

    [Fact]
    public void When_processing_chedp_but_with_ced_notification_Then_should_return_expected_decisions()
    {
        // Arrange
        var decisionContext = new DecisionContextV2(
            [
                new DecisionImportPreNotification()
                {
                    Id = "CHEDPP.GB.2025.9200009",
                    Status = ImportNotificationStatus.Validated,
                    ImportNotificationType = ImportNotificationType.Cvedp,
                    UpdatedSource = DateTime.UtcNow,
                    ConsignmentDecision = ConsignmentDecision.AcceptableForInternalMarket,
                    NotAcceptableAction = null,
                    IuuCheckRequired = true,
                    InspectionRequired = "Not required",
                    IuuOption = "IUUOK",
                    NotAcceptableReasons = null,
                    CommodityChecks =
                    [
                        new DecisionCommodityCheck.Check() { Type = "PHSI_DOCUMENT", Status = "To do" },
                        new DecisionCommodityCheck.Check() { Type = "PHSI_IDENTITY", Status = "To do" },
                        new DecisionCommodityCheck.Check() { Type = "PHSI_PHYSICAL", Status = "To do" },
                        new DecisionCommodityCheck.Check() { Type = "HMI", Status = "Auto cleared" },
                    ],
                    HasPartTwo = true,
                    Commodities =
                    [
                        new DecisionCommodityComplement()
                        {
                            Id = 1,
                            Weight = 56,
                            CommodityCode = "020711",
                        },
                    ],
                },
            ],
            [
                new CustomsDeclarationWrapper(
                    "25GB99999999999021",
                    new CustomsDeclaration()
                    {
                        ClearanceRequest = new ClearanceRequest
                        {
                            Commodities =
                            [
                                new Commodity
                                {
                                    ItemNumber = 1,
                                    TaricCommodityCode = "0207119000",
                                    NetMass = 56,
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
                                        new CommodityCheck { CheckCode = "H222", DepartmentCode = "PHSI" },
                                        new CommodityCheck { CheckCode = "H223", DepartmentCode = "PHSI" },
                                    ],
                                },
                            ],
                        },
                    }
                ),
            ]
        );

        var sut = new DecisionServiceV2(
            new ClearanceDecisionBuilder(new TestCorrelationIdGenerator("TEST")),
            new CheckProcessor(new TestDecisionRulesEngineFactory())
        );

        // Act
        var decisionResult = sut.Process(decisionContext);

        // Assert
        decisionResult.Should().NotBeNull();
        decisionResult[0].Decision.Results?.Length.Should().Be(2);
        decisionResult[0].Decision.Results?[0].CheckCode.Should().Be("H222");
        decisionResult[0].Decision.Results?[0].DecisionCode.Should().Be(DecisionCode.C03.ToString());
        decisionResult[0].Decision.Results?[0].DocumentCode.Should().Be("N853");

        decisionResult[0].Decision.Results?[1].CheckCode.Should().Be("H223");
        decisionResult[0].Decision.Results?[1].DecisionCode.Should().Be(DecisionCode.X00.ToString());
        decisionResult[0].Decision.Results?[1].DocumentCode.Should().Be("C678");
        decisionResult[0]
            .Decision.Results?[1].InternalDecisionCode.Should()
            .Be(DecisionInternalFurtherDetail.E84.ToString());
    }

    private static DecisionContextV2 CreateChedppDecisionContextV2(
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

        return new DecisionContextV2(
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
                    HasPartTwo = true,
                },
            ],
            [
                new CustomsDeclarationWrapper(
                    "25GB12345678901234",
                    new CustomsDeclaration()
                    {
                        ClearanceRequest = new ClearanceRequest
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
                        },
                    }
                ),
            ]
        );
    }
}
