using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.DecisionEngine.DecisionRules;

public class CommodityCodeDecisionRuleTests
{
    private CommodityCodeDecisionRule _rule = new(Options.Create(new DecisionRulesOptions()));

    private readonly DecisionRuleDelegate _mockNext = Substitute.For<DecisionRuleDelegate>();
    private readonly ILogger _mockLogger = Substitute.For<ILogger>();

    // Initialize the rule, mock context, mock next delegate, and logger

    [Fact]
    public void Execute_WhenResultCodeIsNotReleaseOrHold_ReturnsNextResult()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .AddCommodity(c => c.WithCommodityCode("321"))
            .Build();

        var c = new DecisionEngineContext(
            new DecisionContext([notification], []),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity() { TaricCommodityCode = "12345", ItemNumber = 1 },
            new CheckCode() { Value = "H221" },
            new ImportDocument()
        )
        {
            Logger = _mockLogger,
        };

        var result = new DecisionEngineResult(DecisionCode.N01, nameof(CommodityCodeDecisionRule));
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
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .AddCommodity(c => c.WithCommodityCode("321"))
            .Build();

        var c = new DecisionEngineContext(
            new DecisionContext([notification], []),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity() { TaricCommodityCode = "12345", ItemNumber = 1 },
            new CheckCode() { Value = "H221" },
            new ImportDocument()
        )
        {
            Logger = _mockLogger,
        };

        // Simulate that the next result is a "Release" or "Hold"
        var result = new DecisionEngineResult(DecisionCode.C02, nameof(CommodityCodeDecisionRule));
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
                    nameof(CommodityCodeDecisionRule),
                    DecisionInternalFurtherDetail.E20,
                    DecisionResultMode.Passive,
                    DecisionRuleLevel.Level2
                )
            );
        _mockNext.Received(1).Invoke(Arg.Any<DecisionEngineContext>());
        ////var msg = GetFormattedMessageFromLoggerCall(_mockLogger);
        ////msg.Should()
        ////    .Contain("Level 2 would have resulted in an X00 as could not match MRN mrn CommodityCode 12345 for Item 1");
    }

    [Fact]
    public void Execute_WhenResultCodeIsReleaseOrHold_AndNoMatchingCommodities_AndLiveMode_ReturnsResult()
    {
        // Arrange
        _rule = new(Options.Create(new DecisionRulesOptions() { Level2Mode = RuleMode.Live }));
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .AddCommodity(c => c.WithCommodityCode("321"))
            .Build();

        var c = new DecisionEngineContext(
            new DecisionContext([notification], []),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity() { TaricCommodityCode = "12345", ItemNumber = 1 },
            new CheckCode() { Value = "H221" },
            new ImportDocument()
        )
        {
            Logger = _mockLogger,
        };

        // Simulate that the next result is a "Release" or "Hold"
        var result = new DecisionEngineResult(DecisionCode.C02, nameof(CommodityCodeDecisionRule));
        _mockNext.Invoke(Arg.Any<DecisionEngineContext>()).Returns(result);

        // Act
        var returnResult = _rule.Execute(c, _mockNext);

        // Assert
        returnResult
            .Should()
            .Be(
                new DecisionEngineResult(
                    DecisionCode.X00,
                    nameof(CommodityCodeDecisionRule),
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
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .AddCommodity(c => c.WithCommodityCode("123"))
            .Build();

        var c = new DecisionEngineContext(
            new DecisionContext([notification], []),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity() { TaricCommodityCode = "12345", ItemNumber = 1 },
            new CheckCode() { Value = "H221" },
            new ImportDocument()
        )
        {
            Logger = _mockLogger,
        };

        // Simulate that the next result is a "Release" or "Hold"
        var result = new DecisionEngineResult(DecisionCode.C02, nameof(CommodityCodeDecisionRule));
        _mockNext.Invoke(Arg.Any<DecisionEngineContext>()).Returns(result);

        // Act
        var returnResult = _rule.Execute(c, _mockNext);

        // Assert
        returnResult.Should().Be(result);
        _mockNext.Received(1).Invoke(Arg.Any<DecisionEngineContext>());
        _mockLogger.ReceivedCalls().Count().Should().Be(0);
    }
}
