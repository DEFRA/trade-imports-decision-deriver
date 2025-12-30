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

public class CedDecisionRuleTests
{
    private readonly CedDecisionRule _rule = new();

    private readonly DecisionRuleDelegate _mockNext = Substitute.For<DecisionRuleDelegate>();

    [Theory]
    [InlineData(ConsignmentDecision.AcceptableForInternalMarket, DecisionCode.C03, null)]
    [InlineData(ConsignmentDecision.AcceptableForNonInternalMarket, DecisionCode.C03, null)]
    [InlineData("Other", DecisionCode.X00, DecisionInternalFurtherDetail.E96)]
    public void Execute_WhenConsignmentDecision_ReturnsExpectedResult(
        string consignmentDecision,
        DecisionCode expectedCode,
        DecisionInternalFurtherDetail? expectedDetail
    )
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.Submitted)
            .WithConsignmentDecision(consignmentDecision)
            .Build();

        var context = new DecisionResolutionContext(
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

        var expectedResult = expectedDetail.HasValue
            ? new DecisionEngineResult(expectedCode, expectedDetail.Value)
            : new DecisionEngineResult(expectedCode);

        // Act
        var result = _rule.Execute(context, _mockNext);

        // Assert
        result.Should().BeEquivalentTo(expectedResult);
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionResolutionContext>());
    }

    [Theory]
    [InlineData(DecisionNotAcceptableAction.Destruction, DecisionCode.N02, null)]
    [InlineData(DecisionNotAcceptableAction.Redispatching, DecisionCode.N04, null)]
    [InlineData(DecisionNotAcceptableAction.Transformation, DecisionCode.N03, null)]
    [InlineData(DecisionNotAcceptableAction.Other, DecisionCode.N07, null)]
    [InlineData("Invalid", DecisionCode.X00, DecisionInternalFurtherDetail.E97)]
    public void Execute_WhenNotAcceptableAction_ReturnsExpectedResult(
        string? action,
        DecisionCode expectedCode,
        DecisionInternalFurtherDetail? expectedDetail
    )
    {
        // Arrange
        var notificationBuilder = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.Submitted);

        if (action != null)
            notificationBuilder = notificationBuilder.WithNotAcceptableAction(action);

        var notification = notificationBuilder.Build();

        var context = new DecisionResolutionContext(
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

        var expectedResult = expectedDetail.HasValue
            ? new DecisionEngineResult(expectedCode, expectedDetail.Value)
            : new DecisionEngineResult(expectedCode);

        // Act
        var result = _rule.Execute(context, _mockNext);

        // Assert
        result.Should().BeEquivalentTo(expectedResult);
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionResolutionContext>());
    }

    [Fact]
    public void Execute_WhenNoConditionsAreMet_ReturnsX00_E99()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder.Create().WithId("Test").Build();
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

        var expectedResult = new DecisionEngineResult(DecisionCode.X00, DecisionInternalFurtherDetail.E99);

        // Act
        var result = _rule.Execute(c, _mockNext);

        // Assert
        result.Should().BeEquivalentTo(expectedResult);
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionResolutionContext>()); // Ensure the next delegate was not called
    }
}
