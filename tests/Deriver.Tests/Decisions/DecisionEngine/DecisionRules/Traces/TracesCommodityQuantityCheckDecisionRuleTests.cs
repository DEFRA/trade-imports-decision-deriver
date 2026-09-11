using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Trade.Gateway.Api.Contract.Certificate;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.DecisionEngine.DecisionRules.Traces;

public class TracesCommodityQuantityCheckDecisionRuleTests
{
    [Theory]
    [InlineData(RuleMode.Live, 1, 1, DecisionCode.C02, null, DecisionResultMode.Active)]
    [InlineData(RuleMode.Live, 1, 2, DecisionCode.X00, DecisionInternalFurtherDetail.E30, DecisionResultMode.Active)]
    [InlineData(RuleMode.Live, 3, 2, DecisionCode.C02, null, DecisionResultMode.Active)]
    [InlineData(RuleMode.DryRun, 1, 1, DecisionCode.C02, null, DecisionResultMode.Active)]
    [InlineData(RuleMode.DryRun, 1, 2, DecisionCode.X00, DecisionInternalFurtherDetail.E30, DecisionResultMode.Passive)]
    [InlineData(RuleMode.DryRun, 3, 2, DecisionCode.C02, null, DecisionResultMode.Active)]
    public void NetWeight_Tests(
        RuleMode ruleMode,
        decimal chedWeight,
        decimal clearanceRequestWeight,
        DecisionCode expectedDecisionCode,
        DecisionInternalFurtherDetail? expectedDecisionInternalFurtherDetail,
        DecisionResultMode expectedDecisionResultMode
    )
    {
        // Arrange
        var ruleOptions = new DecisionRulesOptions()
        {
            Level3Mode = ruleMode,
            CommodityQuantityCheckDecisionRule =
                TestDecisionRulesEngineFactory.CreateCommodityQuantityCheckDecisionRuleOptions(),
        };
        var rule = new TracesCommodityQuantityCheckDecisionRule();

        var ched = CreateChed("123", netWeight: chedWeight, netWeightUnitCode: "KGM");

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

        var mockNext = CreateMockNext(
            new DecisionEngineResult(
                DecisionCode.C02,
                nameof(TracesCommodityQuantityCheckDecisionRule),
                Level: DecisionRuleLevel.Level3
            )
        );

        var c = new DecisionEngineContext(
            new DecisionContext([], [customsDeclaration], [ched]),
            ruleOptions,
            null!,
            customsDeclaration,
            customsDeclaration.CustomsDeclaration.ClearanceRequest?.Commodities![0]!,
            new CheckCode() { Value = "H222" },
            customsDeclaration.CustomsDeclaration.ClearanceRequest?.Commodities![0]!.Documents![0],
            ched
        )
        {
            Logger = NullLogger.Instance,
            Level2Succeeded = true,
        };

        // Act
        var returnedResult = rule.Execute(c, mockNext);

        // Assert
        var resultToAssert =
            ruleMode == RuleMode.DryRun && returnedResult.PassiveResults is not null
                ? returnedResult.PassiveResults![0]
                : returnedResult;
        resultToAssert
            .Should()
            .BeEquivalentTo(
                new DecisionEngineResult(
                    expectedDecisionCode,
                    nameof(TracesCommodityQuantityCheckDecisionRule),
                    expectedDecisionInternalFurtherDetail,
                    expectedDecisionResultMode,
                    DecisionRuleLevel.Level3
                )
            );

        // A Live-mode hard failure short-circuits before Level3Succeeded is set; every other
        // outcome (a pass, or a DryRun passive failure) lets it fall through as succeeded.
        var expectedLevel3Succeeded = ruleMode != RuleMode.Live || expectedDecisionCode != DecisionCode.X00;
        c.Level3Succeeded.Should().Be(expectedLevel3Succeeded ? true : null);
    }

    [Theory]
    [InlineData(RuleMode.Live, 1, 1, DecisionCode.C02, null, DecisionResultMode.Active)]
    [InlineData(RuleMode.Live, 1, 2, DecisionCode.X00, DecisionInternalFurtherDetail.E31, DecisionResultMode.Active)]
    [InlineData(RuleMode.Live, 3, 2, DecisionCode.C02, null, DecisionResultMode.Active)]
    [InlineData(RuleMode.DryRun, 1, 1, DecisionCode.C02, null, DecisionResultMode.Active)]
    [InlineData(RuleMode.DryRun, 1, 2, DecisionCode.X00, DecisionInternalFurtherDetail.E31, DecisionResultMode.Passive)]
    [InlineData(RuleMode.DryRun, 3, 2, DecisionCode.C02, null, DecisionResultMode.Active)]
    public void ItemQuantity_Tests(
        RuleMode ruleMode,
        int chedItemQuantity,
        int clearanceRequestQuantity,
        DecisionCode expectedDecisionCode,
        DecisionInternalFurtherDetail? expectedDecisionInternalFurtherDetail,
        DecisionResultMode expectedDecisionResultMode
    )
    {
        // Arrange
        var ruleOptions = new DecisionRulesOptions()
        {
            Level3Mode = ruleMode,
            CommodityQuantityCheckDecisionRule =
                TestDecisionRulesEngineFactory.CreateCommodityQuantityCheckDecisionRuleOptions(),
        };
        var rule = new TracesCommodityQuantityCheckDecisionRule();

        var ched = CreateChed("123", itemQuantity: chedItemQuantity);

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

        var mockNext = CreateMockNext(
            new DecisionEngineResult(
                DecisionCode.C02,
                nameof(TracesCommodityQuantityCheckDecisionRule),
                Level: DecisionRuleLevel.Level3
            )
        );

        var c = new DecisionEngineContext(
            new DecisionContext([], [customsDeclaration], [ched]),
            ruleOptions,
            null!,
            customsDeclaration,
            customsDeclaration.CustomsDeclaration.ClearanceRequest?.Commodities![0]!,
            new CheckCode() { Value = "H222" },
            customsDeclaration.CustomsDeclaration.ClearanceRequest?.Commodities![0]!.Documents![0],
            ched
        )
        {
            Logger = NullLogger.Instance,
            Level2Succeeded = true,
        };

        // Act
        var returnedResult = rule.Execute(c, mockNext);

        // Assert
        var resultToAssert =
            ruleMode == RuleMode.DryRun && returnedResult.PassiveResults is not null
                ? returnedResult.PassiveResults![0]
                : returnedResult;
        resultToAssert
            .Should()
            .BeEquivalentTo(
                new DecisionEngineResult(
                    expectedDecisionCode,
                    nameof(TracesCommodityQuantityCheckDecisionRule),
                    expectedDecisionInternalFurtherDetail,
                    expectedDecisionResultMode,
                    DecisionRuleLevel.Level3
                )
            );

        // A Live-mode hard failure short-circuits before Level3Succeeded is set; every other
        // outcome (a pass, or a DryRun passive failure) lets it fall through as succeeded.
        var expectedLevel3Succeeded = ruleMode != RuleMode.Live || expectedDecisionCode != DecisionCode.X00;
        c.Level3Succeeded.Should().Be(expectedLevel3Succeeded ? true : null);
    }

    [Fact]
    public void Execute_WhenResultCodeIsNotReleaseOrHold_ReturnsResultFromNextDelegate()
    {
        // Arrange
        var ruleOptions = new DecisionRulesOptions()
        {
            Level3Mode = RuleMode.DryRun,
            CommodityQuantityCheckDecisionRule =
                TestDecisionRulesEngineFactory.CreateCommodityQuantityCheckDecisionRuleOptions(),
        };
        var rule = new TracesCommodityQuantityCheckDecisionRule();

        var result = new DecisionEngineResult(
            DecisionCode.X00,
            nameof(TracesCommodityQuantityCheckDecisionRule),
            DecisionInternalFurtherDetail.E99
        );
        var mockNext = CreateMockNext(result);

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
        var returnedResult = rule.Execute(c, mockNext);

        // Assert
        returnedResult.Should().BeEquivalentTo(result);
        c.Level3Succeeded.Should().BeNull();
    }

    [Fact]
    public void Execute_WhenLevel2Failed_ReturnsResultFromNextDelegate()
    {
        // Arrange
        var ruleOptions = new DecisionRulesOptions()
        {
            Level3Mode = RuleMode.DryRun,
            CommodityQuantityCheckDecisionRule =
                TestDecisionRulesEngineFactory.CreateCommodityQuantityCheckDecisionRuleOptions(),
        };
        var rule = new TracesCommodityQuantityCheckDecisionRule();

        var result = new DecisionEngineResult(DecisionCode.C02, nameof(TracesCommodityQuantityCheckDecisionRule));
        var mockNext = CreateMockNext(result);

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
            Level2Succeeded = false,
        };

        // Act
        var returnedResult = rule.Execute(c, mockNext);

        // Assert
        returnedResult.Should().BeEquivalentTo(result);
        c.Level3Succeeded.Should().BeNull();
    }

    [Fact]
    public void Execute_WhenChedHasMultipleConsignmentItemsWithMatchingCommodity_SumsTheValuesReturnsResult()
    {
        // Arrange
        var ruleOptions = new DecisionRulesOptions()
        {
            Level3Mode = RuleMode.DryRun,
            CommodityQuantityCheckDecisionRule =
                TestDecisionRulesEngineFactory.CreateCommodityQuantityCheckDecisionRuleOptions(),
        };
        var rule = new TracesCommodityQuantityCheckDecisionRule();

        var ched = new DefraUNVTDCHEDProfile()
        {
            ExchangedDocument = new ExchangedDocument() { Identifier = "test" },
            SpecifiedConsignment = new Consignment()
            {
                IncludedConsignmentItem =
                [
                    ConsignmentItemWith("0207146000", netWeight: 3750, netWeightUnitCode: "KGM"),
                    ConsignmentItemWith("0207146000", netWeight: 15870, netWeightUnitCode: "KGM"),
                ],
            },
        };

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
                            NetMass = 19620,
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
                    ],
                },
            }
        );

        var result = new DecisionEngineResult(
            DecisionCode.C02,
            nameof(TracesCommodityQuantityCheckDecisionRule),
            DecisionInternalFurtherDetail.E99
        );
        var mockNext = CreateMockNext(result);

        var c = new DecisionEngineContext(
            new DecisionContext([], [customsDeclaration], [ched]),
            ruleOptions,
            null!,
            customsDeclaration,
            customsDeclaration.CustomsDeclaration.ClearanceRequest?.Commodities![0]!,
            new CheckCode() { Value = "H222" },
            customsDeclaration.CustomsDeclaration.ClearanceRequest?.Commodities![0]!.Documents![0],
            ched
        )
        {
            Logger = NullLogger.Instance,
            Level2Succeeded = true,
        };

        // Act
        var returnedResult = rule.Execute(c, mockNext);

        // Assert
        returnedResult.Should().BeEquivalentTo(result);
        c.Level3Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("GRM", 5000)]
    [InlineData("TNE", 1)]
    [InlineData("DTN", 10)]
    public void Execute_WhenNetWeightUnitIsNotKgm_ConvertsToKgmBeforeComparing(
        string unitCode,
        decimal chedWeightInUnit
    )
    {
        // Arrange: chedWeightInUnit converts to >= 4kg, but the raw (unconverted) number would not
        var ruleOptions = new DecisionRulesOptions()
        {
            Level3Mode = RuleMode.Live,
            CommodityQuantityCheckDecisionRule =
                TestDecisionRulesEngineFactory.CreateCommodityQuantityCheckDecisionRuleOptions(),
        };
        var rule = new TracesCommodityQuantityCheckDecisionRule();

        var ched = CreateChed("123", netWeight: chedWeightInUnit, netWeightUnitCode: unitCode);

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
                            NetMass = 4,
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

        var mockNext = CreateMockNext(
            new DecisionEngineResult(
                DecisionCode.C02,
                nameof(TracesCommodityQuantityCheckDecisionRule),
                Level: DecisionRuleLevel.Level3
            )
        );

        var c = new DecisionEngineContext(
            new DecisionContext([], [customsDeclaration], [ched]),
            ruleOptions,
            null!,
            customsDeclaration,
            customsDeclaration.CustomsDeclaration.ClearanceRequest?.Commodities![0]!,
            new CheckCode() { Value = "H222" },
            customsDeclaration.CustomsDeclaration.ClearanceRequest?.Commodities![0]!.Documents![0],
            ched
        )
        {
            Logger = NullLogger.Instance,
            Level2Succeeded = true,
        };

        // Act
        var returnedResult = rule.Execute(c, mockNext);

        // Assert: passes because the converted weight (>= 4kg) covers the MRN's declared 4kg
        returnedResult.Code.Should().Be(DecisionCode.C02);
        c.Level3Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Execute_WhenNetWeightUnitIsUnrecognised_ExcludesItFromComparison()
    {
        // Arrange
        var ruleOptions = new DecisionRulesOptions()
        {
            Level3Mode = RuleMode.Live,
            CommodityQuantityCheckDecisionRule =
                TestDecisionRulesEngineFactory.CreateCommodityQuantityCheckDecisionRuleOptions(),
        };
        var rule = new TracesCommodityQuantityCheckDecisionRule();

        var ched = CreateChed("123", netWeight: 999999, netWeightUnitCode: "UNKNOWN");

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
                            NetMass = 1,
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

        var mockNext = CreateMockNext(
            new DecisionEngineResult(
                DecisionCode.C02,
                nameof(TracesCommodityQuantityCheckDecisionRule),
                Level: DecisionRuleLevel.Level3
            )
        );

        var c = new DecisionEngineContext(
            new DecisionContext([], [customsDeclaration], [ched]),
            ruleOptions,
            null!,
            customsDeclaration,
            customsDeclaration.CustomsDeclaration.ClearanceRequest?.Commodities![0]!,
            new CheckCode() { Value = "H222" },
            customsDeclaration.CustomsDeclaration.ClearanceRequest?.Commodities![0]!.Documents![0],
            ched
        )
        {
            Logger = NullLogger.Instance,
            Level2Succeeded = true,
        };

        // Act
        var returnedResult = rule.Execute(c, mockNext);

        // Assert: an unrecognised unit is not trusted as-is, so the (huge) raw number is excluded,
        // leaving 0kg declared on the CHED versus 1kg on the MRN - a failure.
        returnedResult.Code.Should().Be(DecisionCode.X00);
        returnedResult.FurtherDetail.Should().Be(DecisionInternalFurtherDetail.E30);
        c.Level3Succeeded.Should().BeNull();
    }

    private static DecisionRuleDelegate CreateMockNext(DecisionEngineResult result)
    {
        return _ => result;
    }

    private static DefraUNVTDCHEDProfile CreateChed(
        string commodityCode,
        decimal? netWeight = null,
        string? netWeightUnitCode = null,
        int? itemQuantity = null
    )
    {
        return new DefraUNVTDCHEDProfile()
        {
            ExchangedDocument = new ExchangedDocument() { Identifier = "test" },
            SpecifiedConsignment = new Consignment()
            {
                IncludedConsignmentItem =
                [
                    ConsignmentItemWith(commodityCode, netWeight, netWeightUnitCode, itemQuantity),
                ],
            },
        };
    }

    private static ConsignmentItem ConsignmentItemWith(
        string commodityCode,
        decimal? netWeight = null,
        string? netWeightUnitCode = null,
        int? itemQuantity = null
    )
    {
        return new ConsignmentItem()
        {
            IncludedTradeLineItem =
            [
                new TradeLineItem()
                {
                    ApplicableClassification =
                    [
                        new ApplicableClassification() { ClassCode = new CodedValue { Value = commodityCode } },
                    ],
                    NetWeight =
                        netWeight == null
                            ? null
                            : new UneceWeightMeasure()
                            {
                                Value = netWeight.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                                UnitCode = netWeightUnitCode,
                            },
                    PhysicalReferencedLogisticsPackage =
                        itemQuantity == null ? null : [new LogisticsPackage() { ItemQuantity = itemQuantity }],
                },
            ],
        };
    }
}
