using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.DecisionEngine.DecisionRules;

public class UnlinkedNotificationDecisionRuleTests
{
    private readonly UnlinkedNotificationDecisionRule _rule = new();
    private readonly DecisionRuleDelegate _mockNext = Substitute.For<DecisionRuleDelegate>();

    // Initialize the rule and mock objects

    [Fact]
    public void Execute_WhenNotificationIsNull_ReturnsUnlinkedDecisionEngineResult()
    {
        // Arrange
        var c = new DecisionEngineContext(
            new DecisionContext([], []),
            null!,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity(),
            new CheckCode() { Value = "H21" },
            new ImportDocument()
        )
        {
            Logger = NullLogger.Instance,
        };

        // Act
        var result = _rule.Execute(c, _mockNext);

        // Assert using FluentAssertions
        result.Should().Be(DecisionEngineResult.Unlinked);

        // Verify the next delegate was NOT called
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionEngineContext>());
    }

    [Fact]
    public void Execute_WhenNotificationIsNotNull_CallsNextDelegate()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder.Create().WithId("Test").Build();
        var c = new DecisionEngineContext(
            new DecisionContext([notification], []),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity(),
            new CheckCode() { Value = "H21" },
            new ImportDocument()
        )
        {
            Logger = NullLogger.Instance,
        };

        var expectedResult = new DecisionEngineResult(DecisionCode.C02, DecisionInternalFurtherDetail.E84);
        _mockNext.Invoke(Arg.Any<DecisionEngineContext>()).Returns(expectedResult);

        // Act
        var result = _rule.Execute(c, _mockNext);

        // Assert using FluentAssertions
        result.Should().Be(expectedResult);

        // Verify the next delegate was called once
        _mockNext.Received(1).Invoke(Arg.Any<DecisionEngineContext>());
    }
}
