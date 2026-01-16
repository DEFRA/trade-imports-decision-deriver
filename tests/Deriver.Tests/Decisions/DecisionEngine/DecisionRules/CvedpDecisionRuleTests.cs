using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.DecisionEngine.DecisionRules;

public class CvedpDecisionRuleTests
{
    private readonly CvedpDecisionRule _rule = new();
    private readonly DecisionRuleDelegate _mockNext = Substitute.For<DecisionRuleDelegate>();

    // ---------- Acceptable consignment decisions ----------
    [Theory]
    [InlineData(ConsignmentDecision.AcceptableForTranshipment, DecisionCode.E03)]
    [InlineData(ConsignmentDecision.AcceptableForTransit, DecisionCode.E03)]
    [InlineData(ConsignmentDecision.AcceptableForSpecificWarehouse, DecisionCode.E03)]
    [InlineData(ConsignmentDecision.AcceptableForInternalMarket, DecisionCode.C03)]
    [InlineData(ConsignmentDecision.AcceptableIfChanneled, DecisionCode.C06)]
    [InlineData("Other", DecisionCode.X00, DecisionInternalFurtherDetail.E96)]
    public void Execute_WhenConsignmentDecision_ReturnsExpectedDecision(
        string decision,
        DecisionCode expectedCode,
        DecisionInternalFurtherDetail? expectedDetail = null
    )
    {
        // Arrange

        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithConsignmentDecision(decision)
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

        var expectedResult = expectedDetail.HasValue
            ? new DecisionEngineResult(expectedCode, expectedDetail.Value)
            : new DecisionEngineResult(expectedCode);

        // Act
        var result = _rule.Execute(c, _mockNext);

        // Assert
        result.Should().BeEquivalentTo(expectedResult);
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionEngineContext>());
    }

    // ---------- Not acceptable actions ----------
    [Theory]
    [InlineData(DecisionNotAcceptableAction.Destruction, DecisionCode.N02)]
    [InlineData(DecisionNotAcceptableAction.Reexport, DecisionCode.N04)]
    [InlineData(DecisionNotAcceptableAction.Transformation, DecisionCode.N03)]
    [InlineData(DecisionNotAcceptableAction.Other, DecisionCode.N07)]
    public void Execute_WhenNotAcceptableAction_ReturnsExpectedDecision(string action, DecisionCode expectedCode)
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithNotAcceptableAction(action)
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

        var expectedResult = new DecisionEngineResult(expectedCode);

        // Act
        var result = _rule.Execute(c, _mockNext);

        // Assert
        result.Should().BeEquivalentTo(expectedResult);
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionEngineContext>());
    }

    // ---------- Not acceptable reasons ----------
    [Fact]
    public void Execute_WhenNotAcceptableReasonsHasItems_ReturnsN04()
    {
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.Submitted)
            .WithNotAcceptableReasons(["Reason1", "Reason2"])
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

        var expectedResult = new DecisionEngineResult(DecisionCode.N04);

        var result = _rule.Execute(c, _mockNext);

        result.Should().BeEquivalentTo(expectedResult);
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionEngineContext>());
    }

    // ---------- Calls next delegate ----------
    [Fact]
    public void Execute_WhenNoConditionsAreMet_CallsNextDelegate()
    {
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
            new Commodity(),
            new CheckCode() { Value = "H221" },
            new ImportDocument()
        )
        {
            Logger = NullLogger.Instance,
        };

        var result = _rule.Execute(c, _mockNext);

        _mockNext.Received(0).Invoke(c);
        result.Code.Should().Be(DecisionCode.X00);
        result.FurtherDetail.Should().Be(DecisionInternalFurtherDetail.E99);
    }
}
