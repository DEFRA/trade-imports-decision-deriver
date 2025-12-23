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

public class InspectionRequiredDecisionRuleTests
{
    private readonly InspectionRequiredDecisionRule _rule = new();
    private readonly DecisionRuleDelegate _mockNext = Substitute.For<DecisionRuleDelegate>();

    // Initialize the rule and mock objects

    [Fact]
    public void Execute_WhenStatusIsNotSubmittedInProgressOrAmend_CallsNextDelegate()
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

        // Act
        var result = _rule.Execute(c, _mockNext);

        // Assert
        result.Should().BeEquivalentTo(_mockNext.Invoke(Arg.Any<DecisionResolutionContext>()));
        _mockNext.Received(1).Invoke(Arg.Any<DecisionResolutionContext>());
    }

    [Fact]
    public void Execute_WhenInspectionRequiredIsNotRequiredOrInconclusive_ReturnsH01()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.Submitted)
            .WithInspectionRequired(InspectionRequired.NotRequired)
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
        result.Code.Should().Be(DecisionCode.H01);

        // Ensure the next delegate was NOT called
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionResolutionContext>());
    }

    [Fact]
    public void Execute_WhenInspectionRequiredIsInconclusive_ReturnsH01()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.Submitted)
            .WithInspectionRequired(InspectionRequired.Inconclusive)
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
        result.Code.Should().Be(DecisionCode.H01);

        // Ensure the next delegate was NOT called
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionResolutionContext>());
    }

    [Fact]
    public void Execute_WhenInspectionRequiredIsRequired_ReturnsH02()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.Submitted)
            .WithInspectionRequired(InspectionRequired.Required)
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
        result.Code.Should().Be(DecisionCode.H02);

        // Ensure the next delegate was NOT called
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionResolutionContext>());
    }

    [Fact]
    public void Execute_WhenCommodityRequiresInspection_ReturnsH02()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.Submitted)
            .WithInspectionRequired(InspectionRequired.Required)
            .AddCommodity(c => c.WithHmiDecision(CommodityRiskResultHmiDecision.Required))
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
        result.Code.Should().Be(DecisionCode.H02);

        // Ensure the next delegate was NOT called
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionResolutionContext>());
    }

    [Fact]
    public void Execute_WhenNoConditionMatches_CallsNextDelegate()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.Submitted)
            .WithInspectionRequired("Other")
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
