using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Trade.Gateway.Api.Contract.Certificate;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.DecisionEngine.DecisionRules.Traces;

public class TracesCommodityCodeDecisionRuleTests
{
    private TracesCommodityCodeDecisionRule _rule = new(Options.Create(new DecisionRulesOptions()));

    private readonly DecisionRuleDelegate _mockNext = Substitute.For<DecisionRuleDelegate>();
    private readonly ILogger _mockLogger = Substitute.For<ILogger>();

    [Fact]
    public void Execute_WhenResultCodeIsNotReleaseOrHold_ReturnsNextResult()
    {
        // Arrange
        var ched = CreateChed("321");

        var c = new DecisionEngineContext(
            new DecisionContext([], [], [ched]),
            new DecisionRulesOptions(),
            null!,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity() { TaricCommodityCode = "12345", ItemNumber = 1 },
            new CheckCode() { Value = "H221" },
            new ImportDocument(),
            ched
        )
        {
            Logger = _mockLogger,
        };

        var result = new DecisionEngineResult(DecisionCode.N01, nameof(TracesCommodityCodeDecisionRule));
        _mockNext.Invoke(Arg.Any<DecisionEngineContext>()).Returns(result);

        // Act
        var returnResult = _rule.Execute(c, _mockNext);

        // Assert
        returnResult.Should().Be(result);
        _mockNext.Received(1).Invoke(Arg.Any<DecisionEngineContext>());
        _mockLogger.ReceivedCalls().Count().Should().Be(0);
    }

    [Fact]
    public void Execute_WhenResultCodeIsReleaseOrHold_AndNoMatchingCommodities_AndDryRunMode_LogsWarning()
    {
        // Arrange
        var ched = CreateChed("321");

        var c = new DecisionEngineContext(
            new DecisionContext([], [], [ched]),
            new DecisionRulesOptions(),
            null!,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity() { TaricCommodityCode = "12345", ItemNumber = 1 },
            new CheckCode() { Value = "H221" },
            new ImportDocument(),
            ched
        )
        {
            Logger = _mockLogger,
        };

        // Simulate that the next result is a "Release" or "Hold"
        var result = new DecisionEngineResult(DecisionCode.C02, nameof(TracesCommodityCodeDecisionRule));
        _mockNext.Invoke(Arg.Any<DecisionEngineContext>()).Returns(result);

        // Act
        var returnResult = _rule.Execute(c, _mockNext);

        // Assert
        returnResult.Should().Be(result);
        returnResult
            .PassiveResults?[0].Should()
            .Be(
                new DecisionEngineResult(
                    DecisionCode.X00,
                    nameof(TracesCommodityCodeDecisionRule),
                    DecisionInternalFurtherDetail.E20,
                    DecisionResultMode.Passive,
                    DecisionRuleLevel.Level2
                )
            );
        _mockNext.Received(1).Invoke(Arg.Any<DecisionEngineContext>());
    }

    [Fact]
    public void Execute_WhenResultCodeIsReleaseOrHold_AndNoMatchingCommodities_AndLiveMode_ReturnsResult()
    {
        // Arrange
        _rule = new(Options.Create(new DecisionRulesOptions() { Level2Mode = RuleMode.Live }));
        var ched = CreateChed("321");

        var c = new DecisionEngineContext(
            new DecisionContext([], [], [ched]),
            new DecisionRulesOptions(),
            null!,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity() { TaricCommodityCode = "12345", ItemNumber = 1 },
            new CheckCode() { Value = "H221" },
            new ImportDocument(),
            ched
        )
        {
            Logger = _mockLogger,
        };

        // Simulate that the next result is a "Release" or "Hold"
        var result = new DecisionEngineResult(DecisionCode.C02, nameof(TracesCommodityCodeDecisionRule));
        _mockNext.Invoke(Arg.Any<DecisionEngineContext>()).Returns(result);

        // Act
        var returnResult = _rule.Execute(c, _mockNext);

        // Assert
        returnResult
            .Should()
            .Be(
                new DecisionEngineResult(
                    DecisionCode.X00,
                    nameof(TracesCommodityCodeDecisionRule),
                    DecisionInternalFurtherDetail.E20,
                    DecisionResultMode.Active,
                    DecisionRuleLevel.Level2
                )
            );
        _mockNext.Received(1).Invoke(Arg.Any<DecisionEngineContext>());
    }

    [Fact]
    public void Execute_WhenResultCodeIsReleaseOrHold_AndCommodityTaricCodeStartsWithMatchingCommodityCode_LogsNoWarning()
    {
        // Arrange
        var ched = CreateChed("123");

        var c = new DecisionEngineContext(
            new DecisionContext([], [], [ched]),
            new DecisionRulesOptions(),
            null!,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity() { TaricCommodityCode = "12345", ItemNumber = 1 },
            new CheckCode() { Value = "H221" },
            new ImportDocument(),
            ched
        )
        {
            Logger = _mockLogger,
        };

        // Simulate that the next result is a "Release" or "Hold"
        var result = new DecisionEngineResult(DecisionCode.C02, nameof(TracesCommodityCodeDecisionRule));
        _mockNext.Invoke(Arg.Any<DecisionEngineContext>()).Returns(result);

        // Act
        var returnResult = _rule.Execute(c, _mockNext);

        // Assert
        returnResult.Should().Be(result);
        _mockNext.Received(1).Invoke(Arg.Any<DecisionEngineContext>());
        _mockLogger.ReceivedCalls().Count().Should().Be(0);
    }

    [Fact]
    public void Execute_WhenResultCodeIsReleaseOrHold_AndChedHasNoCommodityCodes_AndDryRunMode_LogsWarning()
    {
        // Arrange
        var ched = new DefraUNVTDCHEDProfile()
        {
            ExchangedDocument = new ExchangedDocument() { Identifier = "test" },
            SpecifiedConsignment = new Consignment(),
        };

        var c = new DecisionEngineContext(
            new DecisionContext([], [], [ched]),
            new DecisionRulesOptions(),
            null!,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity() { TaricCommodityCode = "12345", ItemNumber = 1 },
            new CheckCode() { Value = "H221" },
            new ImportDocument(),
            ched
        )
        {
            Logger = _mockLogger,
        };

        var result = new DecisionEngineResult(DecisionCode.C02, nameof(TracesCommodityCodeDecisionRule));
        _mockNext.Invoke(Arg.Any<DecisionEngineContext>()).Returns(result);

        // Act
        var returnResult = _rule.Execute(c, _mockNext);

        // Assert
        returnResult
            .PassiveResults?[0].Should()
            .Be(
                new DecisionEngineResult(
                    DecisionCode.X00,
                    nameof(TracesCommodityCodeDecisionRule),
                    DecisionInternalFurtherDetail.E20,
                    DecisionResultMode.Passive,
                    DecisionRuleLevel.Level2
                )
            );
    }

    [Fact]
    public void Execute_WhenResultCodeIsReleaseOrHold_AndNoMatchingCommodities_AndRuleForEUIsDisabled_AndDryRunMode_ReturnsNextResult()
    {
        // Arrange
        var ched = CreateChed("321");

        var c = new DecisionEngineContext(
            new DecisionContext([], [], [ched]),
            new DecisionRulesOptions()
            {
                Cheds = new Dictionary<string, DecisionRulesPerChedOptions>()
                {
                    {
                        "CVEDA",
                        new DecisionRulesPerChedOptions() { DisabledForEu = [nameof(TracesCommodityCodeDecisionRule)] }
                    },
                },
            },
            null!,
            new CustomsDeclarationWrapper(
                "mrn",
                new CustomsDeclaration() { ClearanceRequest = new ClearanceRequest() { DispatchCountryCode = "BE" } }
            ),
            new Commodity() { TaricCommodityCode = "12345", ItemNumber = 1 },
            new CheckCode() { Value = "H221" },
            new ImportDocument(),
            ched
        )
        {
            Logger = _mockLogger,
        };

        // Simulate that the next result is a "Release" or "Hold"
        var result = new DecisionEngineResult(DecisionCode.C02, nameof(TracesCommodityCodeDecisionRule));
        _mockNext.Invoke(Arg.Any<DecisionEngineContext>()).Returns(result);

        // Act
        var returnResult = _rule.Execute(c, _mockNext);

        // Assert
        returnResult.Should().Be(result);
        _mockNext.Received(1).Invoke(Arg.Any<DecisionEngineContext>());
    }

    [Fact]
    public void Execute_WhenResultCodeIsReleaseOrHold_AndNoMatchingCommodities_AndRuleForRoWIsDisabled_AndDryRunMode_ReturnsNextResult()
    {
        // Arrange
        var ched = CreateChed("321");

        var c = new DecisionEngineContext(
            new DecisionContext([], [], [ched]),
            new DecisionRulesOptions()
            {
                Cheds = new Dictionary<string, DecisionRulesPerChedOptions>()
                {
                    {
                        "CVEDA",
                        new DecisionRulesPerChedOptions() { DisabledForRoW = [nameof(TracesCommodityCodeDecisionRule)] }
                    },
                },
            },
            null!,
            new CustomsDeclarationWrapper(
                "mrn",
                new CustomsDeclaration() { ClearanceRequest = new ClearanceRequest() { DispatchCountryCode = "TT" } }
            ),
            new Commodity() { TaricCommodityCode = "12345", ItemNumber = 1 },
            new CheckCode() { Value = "H221" },
            new ImportDocument(),
            ched
        )
        {
            Logger = _mockLogger,
        };

        // Simulate that the next result is a "Release" or "Hold"
        var result = new DecisionEngineResult(DecisionCode.C02, nameof(TracesCommodityCodeDecisionRule));
        _mockNext.Invoke(Arg.Any<DecisionEngineContext>()).Returns(result);

        // Act
        var returnResult = _rule.Execute(c, _mockNext);

        // Assert
        returnResult.Should().Be(result);
        _mockNext.Received(1).Invoke(Arg.Any<DecisionEngineContext>());
    }

    private static DefraUNVTDCHEDProfile CreateChed(string commodityCode)
    {
        return new DefraUNVTDCHEDProfile()
        {
            ExchangedDocument = new ExchangedDocument() { Identifier = "test" },
            SpecifiedConsignment = new Consignment()
            {
                IncludedConsignmentItem =
                [
                    new ConsignmentItem()
                    {
                        IncludedTradeLineItem =
                        [
                            new TradeLineItem()
                            {
                                ApplicableClassification =
                                [
                                    new ApplicableClassification()
                                    {
                                        ClassCode = new CodedValue { Value = commodityCode },
                                    },
                                ],
                            },
                        ],
                    },
                ],
            },
        };
    }
}
