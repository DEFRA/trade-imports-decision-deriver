using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Microsoft.Extensions.Logging.Abstractions;
using Trade.Gateway.Api.Contract.Certificate;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.DecisionEngine.DecisionRules.Traces;

public class TracesCvedaDecisionRuleTests
{
    [Theory]
    [InlineData(TracesNotificationStatus.Validated, DecisionCode.C03)]
    [InlineData("7000", DecisionCode.H01)]
    public void Execute_Test(string status, DecisionCode expected)
    {
        var ched = new DefraUNVTDCHEDProfile()
        {
            ExchangedDocument = new ExchangedDocument() { NotificationStatusCode = status, Identifier = "test" },
            SpecifiedConsignment = new Consignment(),
        };
        var context = new DecisionEngineContext(
            new DecisionContext([], [], [ched]),
            new DecisionRulesOptions(),
            null!,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity(),
            new CheckCode() { Value = "H221" },
            new ImportDocument(),
            new DefraUNVTDCHEDProfile()
            {
                ExchangedDocument = new ExchangedDocument() { NotificationStatusCode = status, Identifier = "test" },
                SpecifiedConsignment = new Consignment(),
            }
        )
        {
            Logger = NullLogger.Instance,
        };

        // Act
        var result = new TracesCvedaDecisionRule().Execute(
            context,
            engineContext => new DecisionEngineResult(DecisionCode.C02, "Test")
        );

        // Assert using FluentAssertions
        result.Code.Should().Be(expected);
    }
}
