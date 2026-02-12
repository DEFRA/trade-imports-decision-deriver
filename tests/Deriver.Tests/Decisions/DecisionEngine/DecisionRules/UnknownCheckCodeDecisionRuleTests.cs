using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.DecisionEngine.DecisionRules;

public class UnknownCheckCodeDecisionRuleTests
{
    private readonly UnknownCheckCodeDecisionRule _rule = new();
    private readonly DecisionRuleDelegate _mockNext = Substitute.For<DecisionRuleDelegate>();

    [Fact]
    public void Execute_AlwaysReturnsX00_WithE88()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder.Create().WithId("Test").Build();

        var context = new DecisionEngineContext(
            new DecisionContext([notification], []),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity(),
            new CheckCode() { Value = "UNKNOWN" },
            null
        )
        {
            Logger = NullLogger.Instance,
        };

        var expected = DecisionEngineResult.Create(DecisionCode.X00, DecisionInternalFurtherDetail.E88);

        // Act
        var result = _rule.Execute(context, _mockNext);

        // Assert
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Execute_DoesNotInvokeNextDelegate()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder.Create().WithId("Test").Build();

        var context = new DecisionEngineContext(
            new DecisionContext([notification], []),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity(),
            new CheckCode() { Value = "H999" },
            null
        )
        {
            Logger = NullLogger.Instance,
        };

        // Make the next delegate return a different result to prove it is not used
        _mockNext.Invoke(Arg.Any<DecisionEngineContext>()).Returns(DecisionEngineResult.Create(DecisionCode.C02));

        // Act
        var result = _rule.Execute(context, _mockNext);

        // Assert result is the rule's own value and next was not invoked
        result
            .Should()
            .BeEquivalentTo(DecisionEngineResult.Create(DecisionCode.X00, DecisionInternalFurtherDetail.E88));
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionEngineContext>());
    }
}
