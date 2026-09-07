using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Microsoft.Extensions.Logging.Abstractions;
using Trade.Gateway.Api.Contract.Certificate;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.DecisionEngine.DecisionRules.Traces;

public class TracesTerminalStatusDecisionRuleTests
{
    [Theory]
    [InlineData(TracesNotificationStatus.Cancelled, DecisionCode.X00, DecisionInternalFurtherDetail.E71)]
    [InlineData(TracesNotificationStatus.Replaced, DecisionCode.X00, DecisionInternalFurtherDetail.E72)]
    [InlineData(TracesNotificationStatus.Deleted, DecisionCode.X00, DecisionInternalFurtherDetail.E73)]
    [InlineData(TracesNotificationStatus.SplitConsignment, DecisionCode.X00, DecisionInternalFurtherDetail.E75)]
    [InlineData(TracesNotificationStatus.Submitted, DecisionCode.C02, null)]
    [InlineData(TracesNotificationStatus.Validated, DecisionCode.C02, null)]
    [InlineData(TracesNotificationStatus.InProgress, DecisionCode.C02, null)]
    public void Execute_Rule(
        string status,
        DecisionCode expectedDecisionCode,
        DecisionInternalFurtherDetail? expectedInternalCode
    )
    {
        // Arrange
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
        var result = new TracesTerminalStatusDecisionRule().Execute(
            context,
            engineContext => new DecisionEngineResult(DecisionCode.C02, "Test")
        );

        // Assert using FluentAssertions
        result.Code.Should().Be(expectedDecisionCode);
        result.FurtherDetail.Should().Be(expectedInternalCode);
    }
}
