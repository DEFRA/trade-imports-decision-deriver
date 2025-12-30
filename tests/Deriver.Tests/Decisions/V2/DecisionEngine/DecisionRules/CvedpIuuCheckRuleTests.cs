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

public class CvedpIuuCheckRuleTests
{
    private readonly CvedpIuuCheckRule _rule = new();
    private readonly DecisionRuleDelegate _mockNext = Substitute.For<DecisionRuleDelegate>();

    // ---------- Non-IUU checks call next ----------
    [Fact]
    public void Execute_WhenCheckCodeIsNotIuu_CallsNextDelegate()
    {
        var notification = DecisionImportPreNotificationBuilder.Create().WithId("Test").Build();
        var c = new DecisionResolutionContext(
            new DecisionContextV2([notification], []),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity(),
            new CheckCode() { Value = "H219" },
            new ImportDocument()
        )
        {
            Logger = NullLogger.Instance,
        };

        _rule.Execute(c, _mockNext);

        _mockNext.Received(1).Invoke(Arg.Any<DecisionResolutionContext>());
    }

    // ---------- IUU checks with IuuCheckRequired = true ----------
    [Theory]
    [InlineData(ControlAuthorityIuuOption.IUUOK, DecisionCode.C07)]
    [InlineData(ControlAuthorityIuuOption.IUUNotCompliant, DecisionCode.X00)]
    [InlineData(ControlAuthorityIuuOption.IUUNA, DecisionCode.C08)]
    [InlineData(null, DecisionCode.H02, DecisionInternalFurtherDetail.E93)]
    public void Execute_WhenCheckCodeIsIuu_AndIuuCheckRequiredIsTrue_ReturnsExpectedDecision(
        string? iuuOption,
        DecisionCode expectedCode,
        DecisionInternalFurtherDetail? expectedDetail = null
    )
    {
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithIuuCheckRequired(true)
            .WithIuuOption(iuuOption)
            .Build();
        var c = new DecisionResolutionContext(
            new DecisionContextV2([notification], []),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity(),
            new CheckCode() { Value = CheckCode.IuuCheckCode },
            new ImportDocument()
        )
        {
            Logger = NullLogger.Instance,
        };

        var expectedResult = expectedDetail.HasValue
            ? new DecisionEngineResult(expectedCode, expectedDetail.Value)
            : new DecisionEngineResult(expectedCode);

        var result = _rule.Execute(c, _mockNext);

        result.Should().BeEquivalentTo(expectedResult);
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionResolutionContext>());
    }

    // ---------- IUU check with IuuCheckRequired = false ----------
    [Fact]
    public void Execute_WhenCheckCodeIsIuu_AndIuuCheckRequiredIsFalse_ReturnsH02_E94()
    {
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithIuuCheckRequired(false)
            .Build();
        var c = new DecisionResolutionContext(
            new DecisionContextV2([notification], []),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity(),
            new CheckCode() { Value = CheckCode.IuuCheckCode },
            new ImportDocument()
        )
        {
            Logger = NullLogger.Instance,
        };

        var expectedResult = new DecisionEngineResult(DecisionCode.H02, DecisionInternalFurtherDetail.E94);

        var result = _rule.Execute(c, _mockNext);

        result.Should().BeEquivalentTo(expectedResult);
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionResolutionContext>());
    }
}
