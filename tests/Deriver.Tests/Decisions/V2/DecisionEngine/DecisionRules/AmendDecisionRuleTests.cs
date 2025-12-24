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

public class AmendDecisionRuleTests
{
    private readonly AmendDecisionRule _rule = new();
    private readonly DecisionRuleDelegate _mockNext = Substitute.For<DecisionRuleDelegate>();

    // Initialize the rule and mock objects

    [Fact]
    public void Execute_WhenStatusIsNotAmend_CallsNextDelegate()
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
    public void Execute_WhenInspectionRequiredIsNotRequiredOrInconclusive_ReturnsH01AndE80()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.Amend)
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

        // Assert
        result.Code.Should().Be(DecisionCode.H01);
        result.FurtherDetail.Should().Be(DecisionInternalFurtherDetail.E80);
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionResolutionContext>()); // Ensure the next delegate was NOT called
    }

    [Fact]
    public void Execute_WhenInspectionRequiredIsInconclusive_ReturnsH01AndE80()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.Amend)
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

        // Assert
        result.Code.Should().Be(DecisionCode.H01);
        result.FurtherDetail.Should().Be(DecisionInternalFurtherDetail.E80);
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionResolutionContext>()); // Ensure the next delegate was NOT called
    }

    [Fact]
    public void Execute_WhenInspectionRequiredIsRequired_ReturnsH02AndE80()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.Amend)
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

        // Assert
        result.Code.Should().Be(DecisionCode.H02);
        result.FurtherDetail.Should().Be(DecisionInternalFurtherDetail.E80);
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionResolutionContext>()); // Ensure the next delegate was NOT called
    }

    [Fact]
    public void Execute_WhenCommodityRequiresInspection_ReturnsH02AndE80()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.Amend)
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

        // Assert
        result.Code.Should().Be(DecisionCode.H02);
        result.FurtherDetail.Should().Be(DecisionInternalFurtherDetail.E80);
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionResolutionContext>()); // Ensure the next delegate was NOT called
    }

    [Fact]
    public void Execute_WhenInspectionRequiredIsOther_ReturnsH01AndE99()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.Amend)
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

        // Act
        var result = _rule.Execute(c, _mockNext);

        // Assert
        result.Code.Should().Be(DecisionCode.H01);
        result.FurtherDetail.Should().Be(DecisionInternalFurtherDetail.E99);
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionResolutionContext>()); // Ensure the next delegate was NOT called
    }
}
