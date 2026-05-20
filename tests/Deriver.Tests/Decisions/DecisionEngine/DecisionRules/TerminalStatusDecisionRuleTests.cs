using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using Microsoft.Extensions.Logging.Abstractions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.DecisionEngine.DecisionRules;

public class TerminalStatusDecisionRuleTests
{
    [Theory]
    [InlineData(ImportNotificationStatus.Cancelled, DecisionCode.X00, DecisionInternalFurtherDetail.E71)]
    [InlineData(ImportNotificationStatus.Replaced, DecisionCode.X00, DecisionInternalFurtherDetail.E72)]
    [InlineData(ImportNotificationStatus.Deleted, DecisionCode.X00, DecisionInternalFurtherDetail.E73)]
    [InlineData(ImportNotificationStatus.SplitConsignment, DecisionCode.X00, DecisionInternalFurtherDetail.E75)]
    [InlineData(ImportNotificationStatus.Modify, DecisionCode.H01, DecisionInternalFurtherDetail.E81)]
    [InlineData(ImportNotificationStatus.Draft, DecisionCode.C02, null)]
    [InlineData(ImportNotificationStatus.Submitted, DecisionCode.C02, null)]
    [InlineData(ImportNotificationStatus.Validated, DecisionCode.C02, null)]
    [InlineData(ImportNotificationStatus.Rejected, DecisionCode.C02, null)]
    [InlineData(ImportNotificationStatus.InProgress, DecisionCode.C02, null)]
    [InlineData(ImportNotificationStatus.Amend, DecisionCode.C02, null)]
    public void Execute_Rule(
        string status,
        DecisionCode expectedDecisionCode,
        DecisionInternalFurtherDetail? expectedInternalCode
    )
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder.Create().WithId("Test").WithStatus(status).Build();
        var context = new DecisionEngineContext(
            new DecisionContext([notification], []),
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
        var result = new TerminalStatusDecisionRule().Execute(context, engineContext => DecisionEngineResult.C02);

        // Assert using FluentAssertions
        result.Code.Should().Be(expectedDecisionCode);
        result.FurtherDetail.Should().Be(expectedInternalCode);
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
        var c = new DecisionEngineContext(
            new DecisionContext([notification], []),
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
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionEngineContext>());
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
        var c = new DecisionEngineContext(
            new DecisionContext([notification], []),
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
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionEngineContext>());
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
        var c = new DecisionEngineContext(
            new DecisionContext([notification], []),
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
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionEngineContext>());
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
        var c = new DecisionEngineContext(
            new DecisionContext([notification], []),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity(),
            new CheckCode() { Value = "H221" },
            new ImportDocument()
        )
        {
            Logger = NullLogger.Instance,
        };

        var expectedResult = new DecisionEngineResult(
            DecisionCode.C02,
            nameof(TerminalStatusDecisionRule),
            DecisionInternalFurtherDetail.E84
        );
        _mockNext.Invoke(Arg.Any<DecisionEngineContext>()).Returns(expectedResult);

        // Act
        var result = _rule.Execute(c, _mockNext);

        // Assert using FluentAssertions
        result.Should().Be(expectedResult);

        // Ensure the next delegate was called once
        _mockNext.Received(1).Invoke(Arg.Any<DecisionEngineContext>());
    }
}
