using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.DecisionEngine.DecisionRules;

public class CvedpIuuCheckRuleTests
{
    private readonly CvedpIuuCheckRule _rule = new();
    private readonly DecisionRuleDelegate _mockNext = Substitute.For<DecisionRuleDelegate>();

    // ---------- Non-IUU checks call next ----------
    [Fact]
    public void Execute_WhenCheckCodeIsNotIuu_CallsNextDelegate()
    {
        var notification = DecisionImportPreNotificationBuilder.Create().WithId("Test").Build();
        var c = CreateContext(notification, new CheckCode() { Value = "H219" });

        _rule.Execute(c, _mockNext);

        _mockNext.Received(1).Invoke(Arg.Any<DecisionEngineContext>());
    }

    // ---------- IuuOption on the notification ----------
    [Theory]
    [InlineData(ControlAuthorityIuuOption.IUUOK, DecisionCode.C07)]
    [InlineData(ControlAuthorityIuuOption.IUUNotCompliant, DecisionCode.X00)]
    [InlineData(ControlAuthorityIuuOption.IUUNA, DecisionCode.C08)]
    [InlineData("Unknown", DecisionCode.H02, DecisionInternalFurtherDetail.E93)]
    public void Execute_WhenNotificationHasIuuOption_ReturnsDecisionFromIuuOption(
        string iuuOption,
        DecisionCode expectedCode,
        DecisionInternalFurtherDetail? expectedDetail = null
    )
    {
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithIuuOption(iuuOption)
            .Build();
        var c = CreateContext(notification, new CheckCode() { Value = CheckCode.IuuCheckCode });

        var result = _rule.Execute(c, _mockNext);

        result
            .Should()
            .BeEquivalentTo(new DecisionEngineResult(expectedCode, nameof(CvedpIuuCheckRule), expectedDetail));
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionEngineContext>());
    }

    // ---------- No IuuOption on the notification ----------
    [Fact]
    public void Execute_WhenNotificationHasNoIuuOption_ReturnsH02_E93()
    {
        var notification = DecisionImportPreNotificationBuilder.Create().WithId("Test").Build();
        var c = CreateContext(notification, new CheckCode() { Value = CheckCode.IuuCheckCode });

        var result = _rule.Execute(c, _mockNext);

        result
            .Should()
            .BeEquivalentTo(
                new DecisionEngineResult(DecisionCode.H02, nameof(CvedpIuuCheckRule), DecisionInternalFurtherDetail.E93)
            );
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionEngineContext>());
    }

    // ---------- IuuCheckRequired does not affect the decision ----------
    [Theory]
    [InlineData(true, ControlAuthorityIuuOption.IUUOK, DecisionCode.C07)]
    [InlineData(false, ControlAuthorityIuuOption.IUUOK, DecisionCode.C07)]
    [InlineData(null, ControlAuthorityIuuOption.IUUOK, DecisionCode.C07)]
    [InlineData(true, ControlAuthorityIuuOption.IUUNA, DecisionCode.C08)]
    [InlineData(false, ControlAuthorityIuuOption.IUUNA, DecisionCode.C08)]
    [InlineData(null, ControlAuthorityIuuOption.IUUNA, DecisionCode.C08)]
    [InlineData(true, ControlAuthorityIuuOption.IUUNotCompliant, DecisionCode.X00)]
    [InlineData(false, ControlAuthorityIuuOption.IUUNotCompliant, DecisionCode.X00)]
    [InlineData(null, ControlAuthorityIuuOption.IUUNotCompliant, DecisionCode.X00)]
    public void Execute_WhenNotificationHasIuuOption_IgnoresIuuCheckRequired(
        bool? iuuCheckRequired,
        string iuuOption,
        DecisionCode expectedCode
    )
    {
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithIuuCheckRequired(iuuCheckRequired)
            .WithIuuOption(iuuOption)
            .Build();
        var c = CreateContext(notification, new CheckCode() { Value = CheckCode.IuuCheckCode });

        var result = _rule.Execute(c, _mockNext);

        result.Should().BeEquivalentTo(new DecisionEngineResult(expectedCode, nameof(CvedpIuuCheckRule)));
    }

    private static DecisionEngineContext CreateContext(
        DecisionImportPreNotification notification,
        CheckCode checkCode
    ) =>
        new(
            new DecisionContext([notification], [], []),
            new DecisionRulesOptions(),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity(),
            checkCode,
            new ImportDocument(),
            null
        )
        {
            Logger = NullLogger.Instance,
        };
}
