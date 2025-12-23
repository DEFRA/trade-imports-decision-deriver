using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Ipaffs.Constants;
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

public class WrongChedTypeDecisionRuleTests
{
    private readonly WrongChedTypeDecisionRule _rule = new();
    private readonly DecisionRuleDelegate _mockNext = Substitute.For<DecisionRuleDelegate>();

    // Initialize the rule and mock objects

    [Fact]
    public void Execute_WhenImportNotificationTypeIsDifferentFromCheckCode_ReturnsWrongChedTypeDecisionResolutionResult()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithImportNotificationType(ImportNotificationType.Chedpp)
            .WithHasPartTwo(false)
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
        result.Should().Be(DecisionResolutionResult.WrongChedType);

        // Verify the next delegate was NOT called
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionResolutionContext>());
    }

    [Fact]
    public void Execute_WhenImportNotificationTypeIsEqualToCheckCode_ReturnsResultFromNextDelegate()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithImportNotificationType(ImportNotificationType.Cveda)
            .WithHasPartTwo(false)
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

        // Verify the next delegate was called once
        _mockNext.Received(1).Invoke(Arg.Any<DecisionResolutionContext>());
    }
}
