using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.DecisionEngine.DecisionRules;

public class CvedaDecisionRuleTests
{
    private readonly CvedaDecisionRule _rule = new();
    private readonly DecisionRuleDelegate _mockNext = Substitute.For<DecisionRuleDelegate>();

    // ---------- ConsignmentDecision Theory ----------
    [Theory]
    [InlineData(ConsignmentDecision.AcceptableForTranshipment, DecisionCode.E03, null)]
    [InlineData(ConsignmentDecision.AcceptableForTransit, DecisionCode.E03, null)]
    [InlineData(ConsignmentDecision.AcceptableForInternalMarket, DecisionCode.C03, null)]
    [InlineData(ConsignmentDecision.AcceptableForTemporaryImport, DecisionCode.C05, null)]
    [InlineData(ConsignmentDecision.HorseReEntry, DecisionCode.C06, null)]
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
            .WithConsignmentDecision(consignmentDecision)
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
            ? DecisionEngineResult.Create(expectedCode, expectedDetail.Value)
            : DecisionEngineResult.Create(expectedCode);

        // Act
        var result = _rule.Execute(c, _mockNext);

        // Assert
        result.Should().BeEquivalentTo(expectedResult);
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionEngineContext>());
    }

    // ---------- NotAcceptableAction Theory ----------
    [Theory]
    [InlineData(DecisionNotAcceptableAction.Euthanasia, DecisionCode.N02, null)]
    [InlineData(DecisionNotAcceptableAction.Slaughter, DecisionCode.N02, null)]
    [InlineData(DecisionNotAcceptableAction.Reexport, DecisionCode.N04, null)]
    [InlineData("INVALID", DecisionCode.X00, DecisionInternalFurtherDetail.E97)]
    public void Execute_WhenNotAcceptableAction_ReturnsExpectedResult(
        string? action,
        DecisionCode expectedCode,
        DecisionInternalFurtherDetail? expectedDetail
    )
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

        var expectedResult = expectedDetail.HasValue
            ? DecisionEngineResult.Create(expectedCode, expectedDetail.Value)
            : DecisionEngineResult.Create(expectedCode);

        // Act
        var result = _rule.Execute(c, _mockNext);

        // Assert
        result.Should().BeEquivalentTo(expectedResult);
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionEngineContext>());
    }

    // ---------- Facts for single-condition scenarios ----------
    [Fact]
    public void Execute_WhenNotAcceptableReasonsHasItems_ReturnsN04()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
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

        var expectedResult = DecisionEngineResult.Create(DecisionCode.N04);

        // Act
        var result = _rule.Execute(c, _mockNext);

        // Assert
        result.Should().BeEquivalentTo(expectedResult);
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionEngineContext>());
    }

    [Fact]
    public void Execute_WhenNoConditionsAreMet_CallsNextDelegate()
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder.Create().WithId("Test").Build();

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
        _rule.Execute(c, _mockNext);

        // Assert
        _mockNext.Received(1).Invoke(c);
    }
}
