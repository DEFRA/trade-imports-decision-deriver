using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine.DecisionRules;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.V2.DecisionEngine.DecisionRules;

public class TerminalStatusDecisionRuleTests
{
    private readonly TerminalStatusDecisionRule _rule = new();
    private readonly DecisionRuleDelegate _mockNext = Substitute.For<DecisionRuleDelegate>();

    // Initialize the rule and mock objects

    [Fact]
    public void Execute_WhenStatusIsCancelled_ReturnsX00AndE71()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.Cancelled)
            .Build();
        var c = new DecisionResolutionContext(
            new DecisionContextV2([notification], []),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity(),
            new CheckCode() { Value = "H221" },
            new ImportDocument()
        )
        {
            Logger = NullLogger.Instance,
        };

        // Act
        var result = _rule.Execute(c, _mockNext);

        // Assert using FluentAssertions
        result.Code.Should().Be(DecisionCode.X00);
        result.FurtherDetail.Should().Be(DecisionInternalFurtherDetail.E71);

        // Ensure the next delegate was NOT called
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionResolutionContext>());
    }

    [Fact]
    public void Execute_WhenStatusIsReplaced_ReturnsX00AndE72()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.Replaced)
            .Build();
        var c = new DecisionResolutionContext(
            new DecisionContextV2([notification], []),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity(),
            new CheckCode() { Value = "H221" },
            new ImportDocument()
        )
        {
            Logger = NullLogger.Instance,
        };

        // Act
        var result = _rule.Execute(c, _mockNext);

        // Assert using FluentAssertions
        result.Code.Should().Be(DecisionCode.X00);
        result.FurtherDetail.Should().Be(DecisionInternalFurtherDetail.E72);

        // Ensure the next delegate was NOT called
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionResolutionContext>());
    }

    [Fact]
    public void Execute_WhenStatusIsDeleted_ReturnsX00AndE73()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.Deleted)
            .Build();
        var c = new DecisionResolutionContext(
            new DecisionContextV2([notification], []),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity(),
            new CheckCode() { Value = "H221" },
            new ImportDocument()
        )
        {
            Logger = NullLogger.Instance,
        };

        // Act
        var result = _rule.Execute(c, _mockNext);

        // Assert using FluentAssertions
        result.Code.Should().Be(DecisionCode.X00);
        result.FurtherDetail.Should().Be(DecisionInternalFurtherDetail.E73);

        // Ensure the next delegate was NOT called
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionResolutionContext>());
    }

    [Fact]
    public void Execute_WhenStatusIsSplitConsignment_ReturnsX00AndE75()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.SplitConsignment)
            .Build();
        var c = new DecisionResolutionContext(
            new DecisionContextV2([notification], []),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity(),
            new CheckCode() { Value = "H221" },
            new ImportDocument()
        )
        {
            Logger = NullLogger.Instance,
        };

        // Act
        var result = _rule.Execute(c, _mockNext);

        // Assert using FluentAssertions
        result.Code.Should().Be(DecisionCode.X00);
        result.FurtherDetail.Should().Be(DecisionInternalFurtherDetail.E75);

        // Ensure the next delegate was NOT called
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionResolutionContext>());
    }

    [Fact]
    public void Execute_WhenStatusIsOther_ReturnsResultFromNextDelegate()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.Submitted)
            .Build();
        var c = new DecisionResolutionContext(
            new DecisionContextV2([notification], []),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity(),
            new CheckCode() { Value = "H221" },
            new ImportDocument()
        )
        {
            Logger = NullLogger.Instance,
        };

        var expectedResult = new DecisionResolutionResult(DecisionCode.C02, DecisionInternalFurtherDetail.E84);
        _mockNext.Invoke(Arg.Any<DecisionResolutionContext>()).Returns(expectedResult);

        // Act
        var result = _rule.Execute(c, _mockNext);

        // Assert using FluentAssertions
        result.Should().Be(expectedResult);

        // Ensure the next delegate was called once
        _mockNext.Received(1).Invoke(Arg.Any<DecisionResolutionContext>());
    }
}
