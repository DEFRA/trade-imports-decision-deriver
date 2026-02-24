using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.DecisionEngine.DecisionRules;

public class CommodityCodeDecisionRuleTests
{
    private readonly CommodityCodeDecisionRule _rule = new();

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

        var result = DecisionEngineResult.Create(DecisionCode.N01);
        _mockNext.Invoke(Arg.Any<DecisionEngineContext>()).Returns(result);

        // Act
        var returnResult = _rule.Execute(c, _mockNext);

        // Assert
        returnResult.Should().Be(result);
        _mockNext.Received(1).Invoke(Arg.Any<DecisionEngineContext>());
        _mockLogger.ReceivedCalls().Count().Should().Be(0);
    }

    [Fact]
    public void Execute_WhenResultCodeIsReleaseOrHold_AndNoMatchingCommodities_LogsWarning()
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
        var result = DecisionEngineResult.Create(DecisionCode.C02); // or DecisionCode.Hold
        _mockNext.Invoke(Arg.Any<DecisionEngineContext>()).Returns(result);

        // Act
        var returnResult = _rule.Execute(c, _mockNext);

        // Assert
        returnResult.Should().Be(result);
        _mockNext.Received(1).Invoke(Arg.Any<DecisionEngineContext>());
        var msg = GetFormattedMessageFromLoggerCall(_mockLogger);
        msg.Should()
            .Contain("Level 2 would have resulted in an X00 as could not match MRN mrn CommodityCode 12345 for Item 1");
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
        var result = DecisionEngineResult.Create(DecisionCode.C02); // or DecisionCode.Hold
        _mockNext.Invoke(Arg.Any<DecisionEngineContext>()).Returns(result);

        // Act
        var returnResult = _rule.Execute(c, _mockNext);

        // Assert
        returnResult.Should().Be(result);
        _mockNext.Received(1).Invoke(Arg.Any<DecisionEngineContext>());
        _mockLogger.ReceivedCalls().Count().Should().Be(0);
    }

    private static string GetFormattedMessageFromLoggerCall(ILogger logger, int callIndex = 0)
    {
        var calls = logger.ReceivedCalls().ToArray();
        calls.Length.Should().BeGreaterThan(callIndex, "expected at least one log call");

        var call = calls[callIndex];
        // ILogger.Log<TState>(LogLevel, EventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        var args = call.GetArguments();
        var state = args[2];
        var ex = args[3] as Exception;
        var formatter = args[4] as Func<object, Exception?, string>;
        return (formatter is null ? state!.ToString() : formatter!(state!, ex))!;
    }
}
