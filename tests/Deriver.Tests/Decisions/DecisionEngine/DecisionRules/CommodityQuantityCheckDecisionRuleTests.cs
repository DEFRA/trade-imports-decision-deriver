using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
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
        var rule = new CommodityQuantityCheckDecisionRule(
            Options.Create(new DecisionRulesOptions() { Level3Mode = ruleMode })
        );
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
            notification,
            customsDeclaration,
            customsDeclaration.CustomsDeclaration.ClearanceRequest?.Commodities![0]!,
            new CheckCode() { Value = "H221" },
            customsDeclaration.CustomsDeclaration.ClearanceRequest?.Commodities![0]!.Documents![0],
            null
        )
        {
            Logger = NullLogger.Instance,
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
        var rule = new CommodityQuantityCheckDecisionRule(
            Options.Create(new DecisionRulesOptions() { Level3Mode = ruleMode })
        );
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
            notification,
            customsDeclaration,
            customsDeclaration.CustomsDeclaration.ClearanceRequest?.Commodities![0]!,
            new CheckCode() { Value = "H221" },
            customsDeclaration.CustomsDeclaration.ClearanceRequest?.Commodities![0]!.Documents![0],
            null
        )
        {
            Logger = NullLogger.Instance,
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
        var rule = new CommodityQuantityCheckDecisionRule(
            Options.Create(new DecisionRulesOptions() { Level3Mode = RuleMode.DryRun })
        );

        var result = new DecisionEngineResult(
            DecisionCode.X00,
            nameof(CommodityQuantityCheckDecisionRule),
            DecisionInternalFurtherDetail.E99
        );
        _mockNext(Arg.Any<DecisionEngineContext>()).Returns(result);

        var c = new DecisionEngineContext(
            new DecisionContext([], [], []),
            null!,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity(),
            new CheckCode() { Value = "H221" },
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
        var rule = new CommodityQuantityCheckDecisionRule(
            Options.Create(new DecisionRulesOptions() { Level3Mode = RuleMode.DryRun })
        );

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
            notification!,
            customsDeclaration,
            customsDeclaration.CustomsDeclaration.ClearanceRequest?.Commodities![0]!,
            new CheckCode() { Value = "H221" },
            customsDeclaration.CustomsDeclaration.ClearanceRequest?.Commodities![0]!.Documents![0],
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
}
