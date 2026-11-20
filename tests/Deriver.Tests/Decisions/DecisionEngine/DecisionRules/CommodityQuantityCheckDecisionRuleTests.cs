using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using HandlebarsDotNet;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.DecisionEngine.DecisionRules;

public class CommodityQuantityCheckDecisionRuleTests
{
    private readonly DecisionRuleDelegate _mockNext = Substitute.For<DecisionRuleDelegate>();

    [Theory]
    [InlineData(RuleMode.Live, 1, 1, DecisionCode.C02, null, DecisionResultMode.Active)]
    [InlineData(RuleMode.Live, 1, 2, DecisionCode.X00, DecisionInternalFurtherDetail.E30, DecisionResultMode.Active)]
    [InlineData(RuleMode.Live, 3, 2, DecisionCode.C02, null, DecisionResultMode.Active)]
    [InlineData(RuleMode.DryRun, 1, 1, DecisionCode.C02, null, DecisionResultMode.Active)]
    [InlineData(RuleMode.DryRun, 1, 2, DecisionCode.X00, DecisionInternalFurtherDetail.E30, DecisionResultMode.Passive)]
    [InlineData(RuleMode.DryRun, 3, 2, DecisionCode.C02, null, DecisionResultMode.Active)]
    public void NetMass_Tests(
        RuleMode ruleMode,
        decimal notificationWeight,
        decimal clearanceRequestWeight,
        DecisionCode expectedDecisionCode,
        DecisionInternalFurtherDetail? expectedDecisionInternalFurtherDetail,
        DecisionResultMode expectedDecisionResultMode
    )
    {
        // Arrange
        var ruleOptions = new DecisionRulesOptions()
        {
            Ipaffs = new DecisionRulesSourceOptions()
            {
                Level3Mode = ruleMode,
                CommodityQuantityCheckDecisionRule =
                    TestDecisionRulesEngineFactory.CreateCommodityQuantityCheckDecisionRuleOptions(),
            },
        };
        var rule = new CommodityQuantityCheckDecisionRule();
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("7654321")
            .WithStatus(ImportNotificationStatus.Submitted)
            .WithInspectionRequired("Other")
            .AddCommodity(c => c.WithWeight(notificationWeight).WithCommodityCode("123"))
            .Build();

        var customsDeclaration = new CustomsDeclarationWrapper(
            "mrn",
            new CustomsDeclaration()
            {
                ClearanceRequest = new ClearanceRequest()
                {
                    Commodities =
                    [
                        new Commodity()
                        {
                            ItemNumber = 1,
                            NetMass = clearanceRequestWeight,
                            TaricCommodityCode = "123",
                            Documents =
                            [
                                new ImportDocument()
                                {
                                    DocumentReference = new ImportDocumentReference("7654321"),
                                    DocumentCode = "C640",
                                },
                            ],
                        },
                    ],
                },
            }
        );

        _mockNext(Arg.Any<DecisionEngineContext>())
            .Returns(
                new DecisionEngineResult(
                    DecisionCode.C02,
                    nameof(CommodityQuantityCheckDecisionRule),
                    Level: DecisionRuleLevel.Level3
                )
            );

        var c = new DecisionEngineContext(
            new DecisionContext([notification], [customsDeclaration], []),
            ruleOptions,
            notification,
            customsDeclaration,
            customsDeclaration.CustomsDeclaration.ClearanceRequest?.Commodities![0]!,
            new CheckCode() { Value = "H222" },
            customsDeclaration.CustomsDeclaration.ClearanceRequest?.Commodities![0]!.Documents![0],
            null
        )
        {
            Logger = NullLogger.Instance,
            Source = Constants.ChedSource.Ipaffs,
        };

        // Act
        var returnedResult = rule.Execute(c, _mockNext);

        // Asser
        var resultToAssert =
            ruleMode == RuleMode.DryRun && returnedResult.PassiveResults is not null
                ? returnedResult.PassiveResults![0]
                : returnedResult;
        resultToAssert
            .Should()
            .BeEquivalentTo(
                new DecisionEngineResult(
                    expectedDecisionCode,
                    nameof(CommodityQuantityCheckDecisionRule),
                    expectedDecisionInternalFurtherDetail,
                    expectedDecisionResultMode,
                    DecisionRuleLevel.Level3
                )
            );
    }

    [Theory]
    [InlineData(RuleMode.Live, 1, 1, DecisionCode.C02, null, DecisionResultMode.Active)]
    [InlineData(RuleMode.Live, 1, 2, DecisionCode.X00, DecisionInternalFurtherDetail.E31, DecisionResultMode.Active)]
    [InlineData(RuleMode.Live, 3, 2, DecisionCode.C02, null, DecisionResultMode.Active)]
    [InlineData(RuleMode.DryRun, 1, 1, DecisionCode.C02, null, DecisionResultMode.Active)]
    [InlineData(RuleMode.DryRun, 1, 2, DecisionCode.X00, DecisionInternalFurtherDetail.E31, DecisionResultMode.Passive)]
    [InlineData(RuleMode.DryRun, 3, 2, DecisionCode.C02, null, DecisionResultMode.Active)]
    public void NetQuantity_Tests(
        RuleMode ruleMode,
        int notificationQuantity,
        int clearanceRequestQuantity,
        DecisionCode expectedDecisionCode,
        DecisionInternalFurtherDetail? expectedDecisionInternalFurtherDetail,
        DecisionResultMode expectedDecisionResultMode
    )
    {
        // Arrange
        var ruleOptions = new DecisionRulesOptions()
        {
            Ipaffs = new DecisionRulesSourceOptions()
            {
                Level3Mode = ruleMode,
                CommodityQuantityCheckDecisionRule =
                    TestDecisionRulesEngineFactory.CreateCommodityQuantityCheckDecisionRuleOptions(),
            },
        };
        var rule = new CommodityQuantityCheckDecisionRule();
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("7654321")
            .WithStatus(ImportNotificationStatus.Submitted)
            .WithInspectionRequired("Other")
            .AddCommodity(c => c.WithQuantity(notificationQuantity).WithCommodityCode("123"))
            .Build();

        var customsDeclaration = new CustomsDeclarationWrapper(
            "mrn",
            new CustomsDeclaration()
            {
                ClearanceRequest = new ClearanceRequest()
                {
                    Commodities =
                    [
                        new Commodity()
                        {
                            ItemNumber = 1,
                            SupplementaryUnits = clearanceRequestQuantity,
                            TaricCommodityCode = "123",
                            Documents =
                            [
                                new ImportDocument()
                                {
                                    DocumentReference = new ImportDocumentReference("7654321"),
                                    DocumentCode = "C640",
                                },
                            ],
                        },
                    ],
                },
            }
        );

        _mockNext(Arg.Any<DecisionEngineContext>())
            .Returns(
                new DecisionEngineResult(
                    DecisionCode.C02,
                    nameof(CommodityQuantityCheckDecisionRule),
                    Level: DecisionRuleLevel.Level3
                )
            );

        var c = new DecisionEngineContext(
            new DecisionContext([notification], [customsDeclaration], []),
            ruleOptions,
            notification,
            customsDeclaration,
            customsDeclaration.CustomsDeclaration.ClearanceRequest?.Commodities![0]!,
            new CheckCode() { Value = "H222" },
            customsDeclaration.CustomsDeclaration.ClearanceRequest?.Commodities![0]!.Documents![0],
            null
        )
        {
            Logger = NullLogger.Instance,
            Source = Constants.ChedSource.Ipaffs,
        };

        // Act
        var returnedResult = rule.Execute(c, _mockNext);

        // Asser
        var resultToAssert =
            ruleMode == RuleMode.DryRun && returnedResult.PassiveResults is not null
                ? returnedResult.PassiveResults![0]
                : returnedResult;
        resultToAssert
            .Should()
            .BeEquivalentTo(
                new DecisionEngineResult(
                    expectedDecisionCode,
                    nameof(CommodityQuantityCheckDecisionRule),
                    expectedDecisionInternalFurtherDetail,
                    expectedDecisionResultMode,
                    DecisionRuleLevel.Level3
                )
            );
    }

    [Fact]
    public void Execute_WhenResultCodeIsNotReleaseOrHold_ReturnsResultFromNextDelegate()
    {
        // Arrange
        var ruleOptions = new DecisionRulesOptions()
        {
            Ipaffs = new DecisionRulesSourceOptions()
            {
                Level3Mode = RuleMode.DryRun,
                CommodityQuantityCheckDecisionRule =
                    TestDecisionRulesEngineFactory.CreateCommodityQuantityCheckDecisionRuleOptions(),
            },
        };
        var rule = new CommodityQuantityCheckDecisionRule();

        var result = new DecisionEngineResult(
            DecisionCode.X00,
            nameof(CommodityQuantityCheckDecisionRule),
            DecisionInternalFurtherDetail.E99
        );
        _mockNext(Arg.Any<DecisionEngineContext>()).Returns(result);

        var c = new DecisionEngineContext(
            new DecisionContext([], [], []),
            ruleOptions,
            null!,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity(),
            new CheckCode() { Value = "H222" },
            new ImportDocument(),
            null
        )
        {
            Logger = NullLogger.Instance,
        };

        // Act
        var returnedResult = rule.Execute(c, _mockNext);

        // Assert
        returnedResult.Should().BeEquivalentTo(result);
    }

    [Fact]
    public void Execute_WhenCustomsDeclarationHasMultipleItemsWithMatchingCommodity_SumsTheValuesReturnsResult()
    {
        // Arrange
        var ruleOptions = new DecisionRulesOptions()
        {
            Ipaffs = new DecisionRulesSourceOptions()
            {
                Level3Mode = RuleMode.DryRun,
                CommodityQuantityCheckDecisionRule =
                    TestDecisionRulesEngineFactory.CreateCommodityQuantityCheckDecisionRuleOptions(),
            },
        };
        var rule = new CommodityQuantityCheckDecisionRule();

        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("1234567")
            .WithStatus(ImportNotificationStatus.Validated)
            .WithInspectionRequired("Other")
            .AddCommodity(c => c.WithWeight(19620).WithCommodityCode("020714"))
            .Build();

        var customsDeclaration = new CustomsDeclarationWrapper(
            "mrn",
            new CustomsDeclaration()
            {
                ClearanceRequest = new ClearanceRequest()
                {
                    Commodities =
                    [
                        new Commodity()
                        {
                            ItemNumber = 1,
                            NetMass = 3750,
                            TaricCommodityCode = "0207146000",
                            Documents =
                            [
                                new ImportDocument()
                                {
                                    DocumentReference = new ImportDocumentReference("1234567"),
                                    DocumentCode = "C640",
                                },
                            ],
                        },
                        new Commodity()
                        {
                            ItemNumber = 2,
                            NetMass = 15870,
                            TaricCommodityCode = "0207146000",
                            Documents =
                            [
                                new ImportDocument()
                                {
                                    DocumentReference = new ImportDocumentReference("1234567"),
                                    DocumentCode = "C640",
                                },
                            ],
                        },
                        new Commodity()
                        {
                            ItemNumber = 2,
                            NetMass = 15870,
                            TaricCommodityCode = "0207146000",
                            Documents =
                            [
                                new ImportDocument()
                                {
                                    DocumentReference = new ImportDocumentReference("7654321"),
                                    DocumentCode = "C640",
                                },
                            ],
                        },
                    ],
                },
            }
        );

        var result = new DecisionEngineResult(
            DecisionCode.C02,
            nameof(CommodityQuantityCheckDecisionRule),
            DecisionInternalFurtherDetail.E99
        );
        _mockNext(Arg.Any<DecisionEngineContext>()).Returns(result);

        var c = new DecisionEngineContext(
            new DecisionContext([notification], [customsDeclaration], []),
            ruleOptions,
            notification!,
            customsDeclaration,
            customsDeclaration.CustomsDeclaration.ClearanceRequest?.Commodities![0]!,
            new CheckCode() { Value = "H222" },
            customsDeclaration.CustomsDeclaration.ClearanceRequest?.Commodities![0]!.Documents![0],
            null
        )
        {
            Logger = NullLogger.Instance,
            Source = Constants.ChedSource.Ipaffs,
        };

        // Act
        var returnedResult = rule.Execute(c, _mockNext);

        // Assert
        returnedResult.Should().BeEquivalentTo(result);
    }

    [Theory]
    [InlineData(0, DecisionCode.X00, DecisionInternalFurtherDetail.E30)]
    [InlineData(1, DecisionCode.X00, DecisionInternalFurtherDetail.E30)]
    [InlineData(2, DecisionCode.C02, null)]
    public void Execute_WhenMrnItemsShareCommodityCodeAcrossMultipleCheds_ComparesWeightAgainstOwnChedOnly(
        int commodityIndex,
        DecisionCode expectedDecisionCode,
        DecisionInternalFurtherDetail? expectedFurtherDetail
    )
    {
        // Arrange
        // Items 1 & 2 reference CHED 1000015 (9,130 + 15,870 = 25,000kg against 19,620kg) so must fail.
        // Item 4 references CHED 1000016 (100kg against 19,620kg) so must pass. CHED 1000016's weight
        // must not be added to CHED 1000015's to cover items 1 & 2.
        var ruleOptions = new DecisionRulesOptions()
        {
            Ipaffs = new DecisionRulesSourceOptions()
            {
                Level3Mode = RuleMode.Live,
                CommodityQuantityCheckDecisionRule =
                    TestDecisionRulesEngineFactory.CreateCommodityQuantityCheckDecisionRuleOptions(),
            },
        };
        var rule = new CommodityQuantityCheckDecisionRule();

        var firstNotification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("1000015")
            .WithStatus(ImportNotificationStatus.Validated)
            .WithInspectionRequired("Other")
            .AddCommodity(c => c.WithWeight(19620).WithCommodityCode("020714"))
            .Build();
        var secondNotification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("1000016")
            .WithStatus(ImportNotificationStatus.Validated)
            .WithInspectionRequired("Other")
            .AddCommodity(c => c.WithWeight(19620).WithCommodityCode("020714"))
            .Build();

        var customsDeclaration = new CustomsDeclarationWrapper(
            "mrn",
            new CustomsDeclaration()
            {
                ClearanceRequest = new ClearanceRequest()
                {
                    Commodities =
                    [
                        CreateCommodity(itemNumber: 1, netMass: 9130, documentReference: "1000015"),
                        CreateCommodity(itemNumber: 2, netMass: 15870, documentReference: "1000015"),
                        CreateCommodity(itemNumber: 4, netMass: 100, documentReference: "1000016"),
                    ],
                },
            }
        );

        _mockNext(Arg.Any<DecisionEngineContext>())
            .Returns(
                new DecisionEngineResult(
                    DecisionCode.C02,
                    nameof(CommodityQuantityCheckDecisionRule),
                    Level: DecisionRuleLevel.Level3
                )
            );

        var commodity = customsDeclaration.CustomsDeclaration.ClearanceRequest!.Commodities![commodityIndex];
        var notification = commodity.Documents![0].DocumentReference!.Value.EndsWith("1000015")
            ? firstNotification
            : secondNotification;

        var c = new DecisionEngineContext(
            new DecisionContext([firstNotification, secondNotification], [customsDeclaration], []),
            ruleOptions,
            notification,
            customsDeclaration,
            commodity,
            new CheckCode() { Value = "H222" },
            commodity.Documents[0],
            null
        )
        {
            Logger = NullLogger.Instance,
            Source = Constants.ChedSource.Ipaffs,
        };

        // Act
        var returnedResult = rule.Execute(c, _mockNext);

        // Assert
        returnedResult.Code.Should().Be(expectedDecisionCode);
        returnedResult.FurtherDetail.Should().Be(expectedFurtherDetail);
    }

    private static Commodity CreateCommodity(int itemNumber, decimal netMass, string documentReference) =>
        new()
        {
            ItemNumber = itemNumber,
            NetMass = netMass,
            TaricCommodityCode = "0207146000",
            Documents =
            [
                new ImportDocument()
                {
                    DocumentReference = new ImportDocumentReference(documentReference),
                    DocumentCode = "C640",
                },
            ],
        };

    [Fact]
    public void Execute_ShouldNotFallback_WhenUseFallbackIsFalse()
    {
        // Arrange
        var ruleOptions = Options.Create(
            new DecisionRulesOptions()
            {
                Ipaffs = new DecisionRulesSourceOptions()
                {
                    Level3Mode = RuleMode.DryRun,
                    CommodityQuantityCheckDecisionRule = new CommodityQuantityCheckDecisionRuleOptions()
                    {
                        ComparisonEntries =
                        [
                            new CommodityQuantityCheckDecisionRuleComparisonEntry
                            {
                                ComparisonType = QuantityComparisonType.Weight,
                                UseFallback = false,
                            },
                        ],
                    },
                },
            }
        );

        var rule = new CommodityQuantityCheckDecisionRule();

        var context = CreateContext(ruleOptions.Value);
        context.Commodity.NetMass = null;
        context.Commodity.SupplementaryUnits = 100;

        var mockResult = new DecisionEngineResult(
            DecisionCode.C02,
            nameof(CommodityQuantityCheckDecisionRule),
            DecisionInternalFurtherDetail.E99
        );
        _mockNext(Arg.Any<DecisionEngineContext>()).Returns(mockResult);

        // Act
        var result = rule.Execute(context, _mockNext);

        // Assert
        result.Code.Should().Be(mockResult.Code);
    }

    [Fact]
    public void Execute_ShouldReturnValid_WhenNoComparisonCanBePerformed()
    {
        var ruleOptions = Options.Create(
            new DecisionRulesOptions()
            {
                Ipaffs = new DecisionRulesSourceOptions()
                {
                    Level3Mode = RuleMode.DryRun,
                    CommodityQuantityCheckDecisionRule = new CommodityQuantityCheckDecisionRuleOptions()
                    {
                        ComparisonEntries =
                        [
                            new CommodityQuantityCheckDecisionRuleComparisonEntry
                            {
                                ComparisonType = QuantityComparisonType.Weight,
                                UseFallback = true,
                            },
                        ],
                    },
                },
            }
        );

        var rule = new CommodityQuantityCheckDecisionRule();

        var context = CreateContext(ruleOptions.Value);

        context.Commodity.NetMass = null;
        context.Commodity.SupplementaryUnits = null;

        // Act
        var result = rule.Execute(context, CreateSuccessfulDelegate());
        result.Code.Should().Be(DecisionCode.C02);
    }

    [Fact]
    public void Execute_ShouldThrow_WhenComparisonTypeIsInvalid()
    {
        var ruleOptions = Options.Create(
            new DecisionRulesOptions()
            {
                Ipaffs = new DecisionRulesSourceOptions()
                {
                    Level3Mode = RuleMode.DryRun,
                    CommodityQuantityCheckDecisionRule = new CommodityQuantityCheckDecisionRuleOptions()
                    {
                        ComparisonEntries =
                        [
                            new CommodityQuantityCheckDecisionRuleComparisonEntry
                            {
                                ComparisonType = (QuantityComparisonType)999,
                                UseFallback = false,
                            },
                        ],
                    },
                },
            }
        );

        var rule = new CommodityQuantityCheckDecisionRule();

        var context = CreateContext(ruleOptions.Value);

        var result = rule.Execute(context, CreateSuccessfulDelegate());
        result.Code.Should().Be(DecisionCode.C02);
    }

    [Fact]
    public void GetBestMatchingRule_ShouldPreferMatchingChedType()
    {
        var rule = new CommodityQuantityCheckDecisionRule();
        var ruleOptions = Options.Create(
            new DecisionRulesOptions()
            {
                Ipaffs = new DecisionRulesSourceOptions()
                {
                    Level3Mode = RuleMode.DryRun,
                    CommodityQuantityCheckDecisionRule = new CommodityQuantityCheckDecisionRuleOptions()
                    {
                        ComparisonEntries =
                        [
                            new CommodityQuantityCheckDecisionRuleComparisonEntry
                            {
                                ChedType = "CHEDP",
                                ComparisonType = QuantityComparisonType.Weight,
                            },
                        ],
                    },
                },
            }
        );

        var context = CreateContext(ruleOptions.Value);
        context.Notification.ImportNotificationType = "CHEDP";

        var result = rule.Execute(context, CreateSuccessfulDelegate());
        result.Code.Should().Be(DecisionCode.C02);
    }

    [Fact]
    public void GetBestMatchingRule_ShouldScoreMatchingCheckCode()
    {
        var rule = new CommodityQuantityCheckDecisionRule();
        var ruleOptions = Options.Create(
            new DecisionRulesOptions()
            {
                Ipaffs = new DecisionRulesSourceOptions()
                {
                    Level3Mode = RuleMode.DryRun,
                    CommodityQuantityCheckDecisionRule = new CommodityQuantityCheckDecisionRuleOptions()
                    {
                        ComparisonEntries =
                        [
                            new CommodityQuantityCheckDecisionRuleComparisonEntry
                            {
                                CheckCode = "H222",
                                ComparisonType = QuantityComparisonType.Weight,
                            },
                        ],
                    },
                },
            }
        );

        var context = CreateContext(ruleOptions.Value);
        context.ImportDocument!.DocumentCode = "N853";

        var result = rule.Execute(context, CreateSuccessfulDelegate());
        result.Code.Should().Be(DecisionCode.C02);
    }

    [Fact]
    public void GetBestMatchingRule_ShouldIgnoreRule_WhenCheckCodeDoesNotMatch()
    {
        var rule = new CommodityQuantityCheckDecisionRule();
        var ruleOptions = new DecisionRulesOptions()
        {
            Ipaffs = new DecisionRulesSourceOptions()
            {
                Level3Mode = RuleMode.DryRun,
                CommodityQuantityCheckDecisionRule = new CommodityQuantityCheckDecisionRuleOptions()
                {
                    ComparisonEntries =
                    [
                        new CommodityQuantityCheckDecisionRuleComparisonEntry { CommodityCode = "N853" },
                        new CommodityQuantityCheckDecisionRuleComparisonEntry(),
                    ],
                },
            },
        };

        var context = CreateContext(ruleOptions);
        context.ImportDocument!.DocumentCode = "C678";

        var result = rule.Execute(context, CreateSuccessfulDelegate());
        result.Code.Should().Be(DecisionCode.C02);
    }

    [Fact]
    public void GetBestMatchingRule_ShouldScoreMatchingCommodityCode()
    {
        var ruleOptions = new DecisionRulesOptions()
        {
            Ipaffs = new DecisionRulesSourceOptions()
            {
                Level3Mode = RuleMode.DryRun,
                CommodityQuantityCheckDecisionRule = new CommodityQuantityCheckDecisionRuleOptions()
                {
                    ComparisonEntries =
                    [
                        new CommodityQuantityCheckDecisionRuleComparisonEntry { CommodityCode = "0207146000" },
                    ],
                },
            },
        };

        var rule = new CommodityQuantityCheckDecisionRule();

        var context = CreateContext(ruleOptions);

        var result = rule.Execute(context, CreateSuccessfulDelegate());
        result.Code.Should().Be(DecisionCode.C02);
    }

    [Fact]
    public void GetBestMatchingRule_ShouldIgnoreRule_WhenCommodityCodeDoesNotMatch()
    {
        var ruleOptions = new DecisionRulesOptions()
        {
            Ipaffs = new DecisionRulesSourceOptions()
            {
                Level3Mode = RuleMode.DryRun,
                CommodityQuantityCheckDecisionRule = new CommodityQuantityCheckDecisionRuleOptions()
                {
                    ComparisonEntries =
                    [
                        new CommodityQuantityCheckDecisionRuleComparisonEntry { CommodityCode = "0201" },
                        new CommodityQuantityCheckDecisionRuleComparisonEntry(),
                    ],
                },
            },
        };
        var rule = new CommodityQuantityCheckDecisionRule();

        var context = CreateContext(ruleOptions);

        var result = rule.Execute(context, CreateSuccessfulDelegate());
        result.Code.Should().Be(DecisionCode.C02);
    }

    private DecisionRuleDelegate CreateSuccessfulDelegate()
    {
        var mockResult = new DecisionEngineResult(
            DecisionCode.C02,
            nameof(CommodityQuantityCheckDecisionRule),
            DecisionInternalFurtherDetail.E99
        );
        _mockNext(Arg.Any<DecisionEngineContext>()).Returns(mockResult);
        return _mockNext;
    }

    private DecisionEngineContext CreateContext(DecisionRulesOptions rulesOptions)
    {
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("1234567")
            .WithStatus(ImportNotificationStatus.Validated)
            .WithInspectionRequired("Other")
            .AddCommodity(c => c.WithWeight(19620).WithCommodityCode("020714"))
            .Build();

        var customsDeclaration = new CustomsDeclarationWrapper(
            "mrn",
            new CustomsDeclaration()
            {
                ClearanceRequest = new ClearanceRequest()
                {
                    Commodities =
                    [
                        new Commodity()
                        {
                            ItemNumber = 1,
                            NetMass = 3750,
                            TaricCommodityCode = "0207146000",
                            Documents =
                            [
                                new ImportDocument()
                                {
                                    DocumentReference = new ImportDocumentReference("1234567"),
                                    DocumentCode = "C640",
                                },
                            ],
                        },
                        new Commodity()
                        {
                            ItemNumber = 2,
                            NetMass = 15870,
                            TaricCommodityCode = "0207146000",
                            Documents =
                            [
                                new ImportDocument()
                                {
                                    DocumentReference = new ImportDocumentReference("1234567"),
                                    DocumentCode = "C640",
                                },
                            ],
                        },
                        new Commodity()
                        {
                            ItemNumber = 2,
                            NetMass = 15870,
                            TaricCommodityCode = "0207146000",
                            Documents =
                            [
                                new ImportDocument()
                                {
                                    DocumentReference = new ImportDocumentReference("7654321"),
                                    DocumentCode = "C640",
                                },
                            ],
                        },
                    ],
                },
            }
        );

        return new DecisionEngineContext(
            new DecisionContext([notification], [], []),
            rulesOptions,
            notification!,
            customsDeclaration,
            customsDeclaration.CustomsDeclaration.ClearanceRequest?.Commodities![0]!,
            new CheckCode() { Value = "H222" },
            customsDeclaration.CustomsDeclaration.ClearanceRequest?.Commodities![0]!.Documents![0],
            null!
        )
        {
            Logger = NullLogger.Instance,
            Source = Constants.ChedSource.Ipaffs,
        };
    }
}
