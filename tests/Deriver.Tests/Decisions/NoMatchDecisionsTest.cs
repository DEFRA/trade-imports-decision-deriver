using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Ipaffs.Constants;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.Processors;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using ClearanceDecisionBuilder = Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.ClearanceDecisionBuilder;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions;

public class NoMatchDecisionsTest
{
    [Fact]
    public void WhenClearanceRequest_HasNotMatch_AndH220Checks_ThenNoDecisionShouldBeGeneratedWithReason()
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

        var sut = new DecisionServiceV2(
            new ClearanceDecisionBuilder(new TestCorrelationIdGenerator("test")),
            new CheckProcessor(new TestDecisionRulesEngineFactory())
        );

        // Act
        var decisionResult = sut.Process(
            new DecisionContextV2(
                new List<DecisionImportPreNotification>(),
                [new CustomsDeclarationWrapper("123", new CustomsDeclaration() { ClearanceRequest = cr })]
            )
        );

        // Assert
        decisionResult.Should().NotBeNull();
        decisionResult[0].Decision.Results?.Length.Should().Be(9);
        decisionResult[0].Decision.Results?[0].DecisionCode.Should().Be(nameof(DecisionCode.X00));
        decisionResult[0]
            .Decision.Results?[0].DecisionReason.Should()
            .Be(DocumentDecisionReasons.ChedNotFound(decisionResult[0].Decision.Results?[0].DocumentReference));
    }

    [Fact]
    public void WhenClearanceRequest_HasNotMatch_AndH224Checks_ThenNoDecisionShouldBeGeneratedWithReason()
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

        var sut = new DecisionServiceV2(
            new ClearanceDecisionBuilder(new TestCorrelationIdGenerator("Test")),
            new CheckProcessor(new TestDecisionRulesEngineFactory())
        );

        // Act
        var decisionResult = sut.Process(
            new DecisionContextV2(
                new List<DecisionImportPreNotification>(),
                [new CustomsDeclarationWrapper("123", new CustomsDeclaration() { ClearanceRequest = cr })]
            )
        );

        // Assert
        decisionResult.Should().NotBeNull();
        decisionResult[0].Decision.Results?.Length.Should().Be(3);
        decisionResult[0].Decision.Results?[0].DecisionCode.Should().Be(nameof(DecisionCode.X00));
        decisionResult[0].Decision.Results?[0].DecisionReason.Should().Be(DocumentDecisionReasons.OrphanCheckCode);
    }

    [Fact]
    public void WhenClearanceRequest_HasNotMatch_AndChecks_ThenNoDecisionShouldBeGeneratedWithReason()
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

        var sut = new DecisionServiceV2(
            new ClearanceDecisionBuilder(new TestCorrelationIdGenerator("Test")),
            new CheckProcessor(new TestDecisionRulesEngineFactory())
        );

        // Act
        var decisionResult = sut.Process(
            new DecisionContextV2(
                new List<DecisionImportPreNotification>(),
                [new CustomsDeclarationWrapper("123", new CustomsDeclaration() { ClearanceRequest = cr })]
            )
        );

        // Assert
        decisionResult.Should().NotBeNull();
        decisionResult[0].Decision.Results?.Length.Should().Be(3);
        decisionResult[0].Decision.Results?[0].DecisionCode.Should().Be(nameof(DecisionCode.X00));
        decisionResult[0]
            .Decision.Results?[0].DecisionReason.Should()
            .Be("CHED reference Test.1234567 cannot be found in IPAFFS. Check that the reference is correct.");
    }

    [Fact]
    public void WhenClearanceRequest_HasNotMatch_AndNoChecks_ThenNoDecisionShouldBeGenerated()
    {
        // Arrange
        var cr = ClearanceRequestFixtures.ClearanceRequestFixture();
        foreach (var commodity in cr.Commodities!)
        {
            commodity.Checks = [];
        }

        var sut = new DecisionServiceV2(
            new ClearanceDecisionBuilder(new TestCorrelationIdGenerator("Test")),
            new CheckProcessor(new TestDecisionRulesEngineFactory())
        );

        // Act
        var decisionResult = sut.Process(
            new DecisionContextV2(
                new List<DecisionImportPreNotification>(),
                [new CustomsDeclarationWrapper("123", new CustomsDeclaration() { ClearanceRequest = cr })]
            )
        );

        // Assert
        decisionResult.Should().NotBeNull();
        decisionResult[0].Decision.Results?.Length.Should().Be(0);
    }

    [Fact]
    public void When_processing_chedpp_phsi_hmi_with_no_notification()
    {
        var decisionContext = new DecisionContextV2(
            [],
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

        var sut = new DecisionServiceV2(
            new ClearanceDecisionBuilder(new TestCorrelationIdGenerator("Test")),
            new CheckProcessor(new TestDecisionRulesEngineFactory())
        );

        var decisionResult = sut.Process(decisionContext);

        decisionResult[0].Decision.Results?.Length.Should().Be(2);
        decisionResult[0].Decision.Results?[0].DecisionCode.Should().Be(nameof(DecisionCode.X00));
        decisionResult[0].Decision.Results?[1].DecisionCode.Should().Be(nameof(DecisionCode.X00));
    }

    [Fact]
    public void When_processing_chedpp_with_new_c085_with_no_notification()
    {
        var decisionContext = new DecisionContextV2(
            [],
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
                                            DocumentCode = "C085",
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

        var sut = new DecisionServiceV2(
            new ClearanceDecisionBuilder(new TestCorrelationIdGenerator("Test")),
            new CheckProcessor(new TestDecisionRulesEngineFactory())
        );

        var decisionResult = sut.Process(decisionContext);

        decisionResult.Count.Should().Be(1);
        decisionResult[0].Decision.Results?[0].DecisionCode.Should().Be(nameof(DecisionCode.X00));
        decisionResult[0]
            .Decision.Results?[0].DecisionReason.Should()
            .Be("CHED reference GBCHD2025.9200009 cannot be found in IPAFFS. Check that the reference is correct.");
    }

    [Fact]
    public void When_processing_chedpp_with_phsi_and_all_three_document_codes_Then_should_return_expected_decisions()
    {
        // Arrange
        var decisionContext = new DecisionContextV2(
            [],
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
                        },
                    }
                ),
            ]
        );

        var sut = new DecisionServiceV2(
            new ClearanceDecisionBuilder(new TestCorrelationIdGenerator("Test")),
            new CheckProcessor(new TestDecisionRulesEngineFactory())
        );

        // Act
        var decisionResult = sut.Process(decisionContext);

        // Assert

        decisionResult.Should().NotBeNull();
        decisionResult[0].Decision.Results?.Length.Should().Be(3);
        decisionResult[0].Decision.Results?[0].CheckCode.Should().Be("H219");
        decisionResult[0].Decision.Results?[0].DecisionCode.Should().Be(nameof(DecisionCode.X00));
        decisionResult[0].Decision.Results?[0].DocumentCode.Should().Be("N851");

        decisionResult[0].Decision.Results?[1].CheckCode.Should().Be("H219");
        decisionResult[0].Decision.Results?[1].DecisionCode.Should().Be(nameof(DecisionCode.X00));
        decisionResult[0].Decision.Results?[1].DocumentCode.Should().Be("C085");

        decisionResult[0].Decision.Results?[2].CheckCode.Should().Be("H219");
        decisionResult[0].Decision.Results?[2].DecisionCode.Should().Be(nameof(DecisionCode.X00));
        decisionResult[0].Decision.Results?[2].DocumentCode.Should().Be("9115");
    }

    [Fact]
    public void When_processing_H220_without_H219_Then_should_return_expected_decisions()
    {
        // Arrange
        var decisionContext = new DecisionContextV2(
            [],
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

                                    Checks = [new CommodityCheck { CheckCode = "H220", DepartmentCode = "HMI" }],
                                },
                            ],
                        },
                    }
                ),
            ]
        );

        var sut = new DecisionServiceV2(
            new ClearanceDecisionBuilder(new TestCorrelationIdGenerator("Test")),
            new CheckProcessor(new TestDecisionRulesEngineFactory())
        );

        // Act
        var decisionResult = sut.Process(decisionContext);

        // Assert

        decisionResult.Should().NotBeNull();
        decisionResult.Count.Should().Be(1);
        decisionResult[0].Decision.Results?[0].CheckCode.Should().Be("H220");
        decisionResult[0].Decision.Results?[0].DecisionCode.Should().Be(nameof(DecisionCode.X00));
        decisionResult[0].Decision.Results?[0].DocumentCode.Should().Be(null);
        decisionResult[0].Decision.Results?[0].DocumentReference.Should().Be(String.Empty);
        decisionResult[0]
            .Decision.Results?[0].InternalDecisionCode.Should()
            .Be(nameof(DecisionInternalFurtherDetail.E87));
        decisionResult[0].Decision.Results?[0].DecisionReason.Should().Be(DocumentDecisionReasons.GmsInspection);
    }

    [Fact]
    public void When_processing_H220_with_H219_Then_should_return_expected_decisions()
    {
        // Arrange
        var decisionContext = new DecisionContextV2(
            [],
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

                                    Checks =
                                    [
                                        new CommodityCheck { CheckCode = "H220", DepartmentCode = "HMI" },
                                        new CommodityCheck { CheckCode = "H219", DepartmentCode = "HMI" },
                                    ],
                                },
                            ],
                        },
                    }
                ),
            ]
        );

        var sut = new DecisionServiceV2(
            new ClearanceDecisionBuilder(new TestCorrelationIdGenerator("Test")),
            new CheckProcessor(new TestDecisionRulesEngineFactory())
        );

        // Act
        var decisionResult = sut.Process(decisionContext);

        // Assert

        decisionResult.Should().NotBeNull();
        decisionResult[0].Decision.Results?.Length.Should().Be(2);
        decisionResult[0].Decision.Results?[0].CheckCode.Should().Be("H220");
        decisionResult[0].Decision.Results?[0].DecisionCode.Should().Be(nameof(DecisionCode.X00));
        decisionResult[0].Decision.Results?[0].DocumentCode.Should().Be(null);
        decisionResult[0].Decision.Results?[0].DocumentReference.Should().Be(String.Empty);
        decisionResult[0]
            .Decision.Results?[0].InternalDecisionCode.Should()
            .Be(nameof(DecisionInternalFurtherDetail.E82));
        decisionResult[0].Decision.Results?[0].DecisionReason.Should().Be(DocumentDecisionReasons.GmsInspectionAmend);
    }

    [Fact]
    public void When_processing_orphan_H221__Then_should_return_expected_decisions()
    {
        // Arrange
        var decisionContext = new DecisionContextV2(
            [],
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

                                    Checks = [new CommodityCheck { CheckCode = "H221", DepartmentCode = "HMI" }],
                                },
                            ],
                        },
                    }
                ),
            ]
        );

        var sut = new DecisionServiceV2(
            new ClearanceDecisionBuilder(new TestCorrelationIdGenerator("Test")),
            new CheckProcessor(new TestDecisionRulesEngineFactory())
        );

        // Act
        var decisionResult = sut.Process(decisionContext);

        // Assert

        decisionResult.Should().NotBeNull();
        decisionResult.Count.Should().Be(1);
        decisionResult[0].Decision.Results?[0].CheckCode.Should().Be("H221");
        decisionResult[0].Decision.Results?[0].DecisionCode.Should().Be(nameof(DecisionCode.X00));
        decisionResult[0].Decision.Results?[0].DocumentCode.Should().Be(null);
        decisionResult[0].Decision.Results?[0].DocumentReference.Should().Be(string.Empty);
        decisionResult[0]
            .Decision.Results?[0].InternalDecisionCode.Should()
            .Be(nameof(DecisionInternalFurtherDetail.E83));
        decisionResult[0].Decision.Results?[0].DecisionReason.Should().Be(DocumentDecisionReasons.OrphanCheckCode);
    }

    [Fact]
    public void When_processing_orphan_check_code_Then_should_return_expected_decisions()
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
                    IuuCheckRequired = null,
                    IuuOption = null,
                    NotAcceptableReasons = null,
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
                        },
                    }
                ),
            ]
        );

        var sut = new DecisionServiceV2(
            new ClearanceDecisionBuilder(new TestCorrelationIdGenerator("Test")),
            new CheckProcessor(new TestDecisionRulesEngineFactory())
        );

        // Act
        var decisionResult = sut.Process(decisionContext);

        // Assert

        decisionResult.Should().NotBeNull();
        decisionResult[0].Decision.Results?.Length.Should().Be(3);
        decisionResult[0].Decision.Results?[0].CheckCode.Should().Be("H222");
        decisionResult[0].Decision.Results?[0].DecisionCode.Should().Be(nameof(DecisionCode.C03));
        decisionResult[0].Decision.Results?[0].DocumentCode.Should().Be("N853");

        decisionResult[0].Decision.Results?[1].CheckCode.Should().Be("H223");
        decisionResult[0].Decision.Results?[1].DecisionCode.Should().Be(nameof(DecisionCode.X00));
        decisionResult[0]
            .Decision.Results?[1].InternalDecisionCode.Should()
            .Be(nameof(DecisionInternalFurtherDetail.E84));

        decisionResult[0].Decision.Results?[2].CheckCode.Should().Be("H219");
        decisionResult[0].Decision.Results?[2].DecisionCode.Should().Be(nameof(DecisionCode.X00));
        decisionResult[0]
            .Decision.Results?[2].InternalDecisionCode.Should()
            .Be(nameof(DecisionInternalFurtherDetail.E83));
    }

    [Fact]
    public void When_processing_iuu_check_codes_Then_should_return_expected_decisions()
    {
        // Arrange
        var decisionContext = new DecisionContextV2(
            [],
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
                        },
                    }
                ),
            ]
        );

        var sut = new DecisionServiceV2(
            new ClearanceDecisionBuilder(new TestCorrelationIdGenerator("Test")),
            new CheckProcessor(new TestDecisionRulesEngineFactory())
        );

        // Act
        var decisionResult = sut.Process(decisionContext);

        // Assert

        decisionResult.Should().NotBeNull();
        decisionResult[0].Decision.Results?.Length.Should().Be(2);
        decisionResult[0].Decision.Results?[0].CheckCode.Should().Be("H222");
        decisionResult[0].Decision.Results?[0].DecisionCode.Should().Be(nameof(DecisionCode.X00));
        decisionResult[0]
            .Decision.Results?[0].InternalDecisionCode.Should()
            .Be(nameof(DecisionInternalFurtherDetail.E70));
        decisionResult[0].Decision.Results?[0].DocumentCode.Should().Be("N853");

        decisionResult[0].Decision.Results?[1].CheckCode.Should().Be("H224");
        decisionResult[0].Decision.Results?[1].DecisionCode.Should().Be(nameof(DecisionCode.X00));
        decisionResult[0]
            .Decision.Results?[1].InternalDecisionCode.Should()
            .Be(nameof(DecisionInternalFurtherDetail.E70));
        decisionResult[0].Decision.Results?[1].DocumentCode.Should().Be("N853");
    }
}
