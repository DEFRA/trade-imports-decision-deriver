using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.DecisionEngine.DecisionRules;

public class CommodityQuantityCheckDecisionRuleTests
{
    private readonly CommodityQuantityCheckDecisionRule _rule = new(Options.Create(new DecisionRulesOptions()));

    private readonly DecisionRuleDelegate _mockNext = Substitute.For<DecisionRuleDelegate>();
    private readonly ILogger _mockLogger = Substitute.For<ILogger>();

    // Initialize the rule, mock context, mock next delegate, and logger

    [Fact]
    public void Execute_WhenResultCodeIsNotReleaseOrHold_ReturnsResultFromNextDelegate()
    {
        // Arrange
        var result = DecisionEngineResult.Create(DecisionCode.X00, DecisionInternalFurtherDetail.E99);
        _mockNext(Arg.Any<DecisionEngineContext>()).Returns(result);

        var c = new DecisionEngineContext(
            new DecisionContext([], []),
            null!,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity(),
            new CheckCode() { Value = "H221" },
            new ImportDocument()
        )
        {
            Logger = NullLogger.Instance,
        };

        // Act
        var returnedResult = _rule.Execute(c, _mockNext);

        // Assert
        returnedResult.Should().BeEquivalentTo(result);
        _mockLogger.ReceivedCalls().Count().Should().Be(0); // Ensure no logging occurred
    }

    [Fact]
    public void Execute_WhenNetMassIsProvided_AndTotalWeightExceedsNetMass_LogsWarning()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.Submitted)
            .WithInspectionRequired("Other")
            .Build();
        var c = new DecisionEngineContext(
            new DecisionContext([notification], []),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity
            {
                NetMass = 100,
                SupplementaryUnits = null,
                TaricCommodityCode = "12345",
            },
            new CheckCode() { Value = "H221" },
            new ImportDocument()
        )
        {
            Logger = _mockLogger,
        };
        var result = DecisionEngineResult.Create(DecisionCode.H01);
        _mockNext(Arg.Any<DecisionEngineContext>()).Returns(result);

        // Act
        _rule.Execute(c, _mockNext);

        // Assert
        _mockLogger.ReceivedCalls().Count().Should().BeGreaterThanOrEqualTo(1);
        var msg = GetFormattedMessageFromLoggerCall(_mockLogger, 0);
        msg.Should().Contain("IPAFFS NetWeight");
    }

    [Fact]
    public void Execute_WhenNetMassIsProvided_AndTotalWeightIsLessThanNetMass_LogsInformation()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.Submitted)
            .WithInspectionRequired("Other")
            .Build();
        var c = new DecisionEngineContext(
            new DecisionContext([notification], []),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity
            {
                NetMass = 200,
                SupplementaryUnits = null,
                TaricCommodityCode = "12345",
            },
            new CheckCode() { Value = "H221" },
            new ImportDocument()
        )
        {
            Logger = _mockLogger,
        };

        var result = DecisionEngineResult.Create(DecisionCode.H01);
        _mockNext(Arg.Any<DecisionEngineContext>()).Returns(result);

        // Act
        _rule.Execute(c, _mockNext);

        // Assert
        _mockLogger.ReceivedCalls().Count().Should().BeGreaterThanOrEqualTo(1);
        var msg = GetFormattedMessageFromLoggerCall(_mockLogger, 0);
        msg.Should().Contain("IPAFFS NetWeight");
    }

    [Fact]
    public void Execute_WhenSupplementaryUnitsIsProvided_AndTotalQuantityExceedsSupplementaryUnits_LogsWarning()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.Submitted)
            .WithInspectionRequired("Other")
            .Build();
        var c = new DecisionEngineContext(
            new DecisionContext([notification], []),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity
            {
                SupplementaryUnits = 100,
                NetMass = null,
                TaricCommodityCode = "12345",
            },
            new CheckCode() { Value = "H221" },
            new ImportDocument()
        )
        {
            Logger = _mockLogger,
        };

        var result = DecisionEngineResult.Create(DecisionCode.H01);
        _mockNext(Arg.Any<DecisionEngineContext>()).Returns(result);

        // Act
        _rule.Execute(c, _mockNext);

        // Assert
        _mockLogger.ReceivedCalls().Count().Should().BeGreaterThanOrEqualTo(1);
        var msg = GetFormattedMessageFromLoggerCall(_mockLogger, 0);
        msg.Should().Contain("IPAFFS NetQuantity");
    }

    [Fact]
    public void Execute_WhenSupplementaryUnitsIsProvided_AndTotalQuantityIsLessThanSupplementaryUnits_LogsInformation()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.Submitted)
            .WithInspectionRequired("Other")
            .Build();
        var c = new DecisionEngineContext(
            new DecisionContext([notification], []),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity
            {
                SupplementaryUnits = 200,
                NetMass = null,
                TaricCommodityCode = "12345",
            },
            new CheckCode() { Value = "H221" },
            new ImportDocument()
        )
        {
            Logger = _mockLogger,
        };

        var result = DecisionEngineResult.Create(DecisionCode.H01);
        _mockNext(Arg.Any<DecisionEngineContext>()).Returns(result);

        // Act
        _rule.Execute(c, _mockNext);

        // Assert

        // Assert
        _mockLogger.ReceivedCalls().Count().Should().BeGreaterThanOrEqualTo(1);
        var msg = GetFormattedMessageFromLoggerCall(_mockLogger, 0);
        msg.Should().Contain("IPAFFS NetQuantity");
    }

    [Fact]
    public void Execute_WhenSupplementaryUnitsIsNullAndNetMassIsNull_DoesNotLog()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.Submitted)
            .WithInspectionRequired("Other")
            .Build();
        var c = new DecisionEngineContext(
            new DecisionContext([notification], []),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity
            {
                SupplementaryUnits = null,
                NetMass = null,
                TaricCommodityCode = "12345",
            },
            new CheckCode() { Value = "H221" },
            new ImportDocument()
        )
        {
            Logger = _mockLogger,
        };

        ////var commodity = new Commodity
        ////{
        ////    SupplementaryUnits = null,
        ////    NetMass = null,
        ////    TaricCommodityCode = "12345"
        ////};

        ////var commodities = new List<DecisionCommodityComplement>(); // Empty commodities
        ////_mockContext.Commodity.Returns(commodity);
        ////_mockContext.Notification.Commodities.Returns(commodities);
        var result = DecisionEngineResult.Create(DecisionCode.H01);
        _mockNext(Arg.Any<DecisionEngineContext>()).Returns(result);

        // Act
        _rule.Execute(c, _mockNext);

        // Assert

        // Assert
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
