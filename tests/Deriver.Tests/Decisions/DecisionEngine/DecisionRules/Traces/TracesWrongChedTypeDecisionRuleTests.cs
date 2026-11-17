using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Trade.Gateway.Api.Contract.Certificate;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.DecisionEngine.DecisionRules.Traces;

public class TracesWrongChedTypeDecisionRuleTests
{
    private readonly TracesWrongChedTypeDecisionRule _rule = new();
    private readonly DecisionRuleDelegate _mockNext = Substitute.For<DecisionRuleDelegate>();

    [Theory]
    [InlineData("H221", "CHEDA.GB.2026.1234567")]
    [InlineData("H223", "CHEDD.GB.2026.1234567")]
    [InlineData("H222", "CHEDP.GB.2026.1234567")]
    [InlineData("H224", "CHEDP.GB.2026.1234567")]
    [InlineData("H218", "CHEDPP.GB.2026.1234567")]
    [InlineData("H219", "CHEDPP.GB.2026.1234567")]
    [InlineData("H220", "CHEDPP.GB.2026.1234567")]
    public void Execute_WhenChedTypeMatchesCheckCode_ReturnsResultFromNextDelegate(
        string checkCode,
        string chedIdentifier
    )
    {
        var context = CreateContext(checkCode, CreateChed(chedIdentifier));
        var expectedResult = new DecisionEngineResult(DecisionCode.C02, "Next");
        _mockNext.Invoke(Arg.Any<DecisionEngineContext>()).Returns(expectedResult);

        var result = _rule.Execute(context, _mockNext);

        result.Should().Be(expectedResult);
        _mockNext.Received(1).Invoke(context);
    }

    [Theory]
    [InlineData("H221", "CHEDP.GB.2026.1234567")]
    [InlineData("H223", "CHEDA.GB.2026.1234567")]
    [InlineData("H222", "CHEDPP.GB.2026.1234567")]
    [InlineData("H224", "CHEDD.GB.2026.1234567")]
    [InlineData("H218", "CHEDP.GB.2026.1234567")]
    [InlineData("H219", "CHEDA.GB.2026.1234567")]
    [InlineData("H220", "CHEDD.GB.2026.1234567")]
    [InlineData("H221", "UNKNOWN.GB.2026.1234567")]
    [InlineData("H221", "")]
    [InlineData("9115", "CHEDA.GB.2026.1234567")]
    public void Execute_WhenChedTypeDoesNotMatchCheckCode_ReturnsWrongChedTypeResult(
        string checkCode,
        string chedIdentifier
    )
    {
        var context = CreateContext(checkCode, CreateChed(chedIdentifier));

        var result = _rule.Execute(context, _mockNext);

        result
            .Should()
            .Be(
                new DecisionEngineResult(
                    DecisionCode.X00,
                    nameof(WrongChedTypeDecisionRule),
                    DecisionInternalFurtherDetail.E84
                )
            );
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionEngineContext>());
    }

    [Fact]
    public void Execute_WhenChedIsNullAndCheckCodeIsKnown_ReturnsWrongChedTypeResult()
    {
        var context = CreateContext("H221", null);

        var result = _rule.Execute(context, _mockNext);

        result.Code.Should().Be(DecisionCode.X00);
        result.FurtherDetail.Should().Be(DecisionInternalFurtherDetail.E84);
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionEngineContext>());
    }

    private static DefraUNVTDCHEDProfile CreateChed(string identifier) =>
        new()
        {
            ExchangedDocument = new ExchangedDocument { Identifier = identifier },
            SpecifiedConsignment = new Consignment(),
        };

    private static DecisionEngineContext CreateContext(string checkCode, DefraUNVTDCHEDProfile? ched) =>
        new(
            new DecisionContext([], [], ched is null ? [] : [ched]),
            new DecisionRulesOptions(),
            null!,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity(),
            new CheckCode { Value = checkCode },
            new ImportDocument(),
            ched
        )
        {
            Logger = NullLogger.Instance,
        };
}
