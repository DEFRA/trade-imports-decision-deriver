using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using Microsoft.Extensions.Logging.Abstractions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.DecisionEngine.DecisionRules;

public class TerminalStatusDecisionRuleTests
{
    [Theory]
    [InlineData(ImportNotificationStatus.Cancelled, DecisionCode.X00, DecisionInternalFurtherDetail.E71)]
    [InlineData(ImportNotificationStatus.Replaced, DecisionCode.X00, DecisionInternalFurtherDetail.E72)]
    [InlineData(ImportNotificationStatus.Deleted, DecisionCode.X00, DecisionInternalFurtherDetail.E73)]
    [InlineData(ImportNotificationStatus.SplitConsignment, DecisionCode.X00, DecisionInternalFurtherDetail.E75)]
    [InlineData(ImportNotificationStatus.Modify, DecisionCode.H01, DecisionInternalFurtherDetail.E81)]
    [InlineData(ImportNotificationStatus.Draft, DecisionCode.C02, null)]
    [InlineData(ImportNotificationStatus.Submitted, DecisionCode.C02, null)]
    [InlineData(ImportNotificationStatus.Validated, DecisionCode.C02, null)]
    [InlineData(ImportNotificationStatus.Rejected, DecisionCode.C02, null)]
    [InlineData(ImportNotificationStatus.InProgress, DecisionCode.C02, null)]
    [InlineData(ImportNotificationStatus.Amend, DecisionCode.C02, null)]
    public void Execute_Rule(
        string status,
        DecisionCode expectedDecisionCode,
        DecisionInternalFurtherDetail? expectedInternalCode
    )
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder.Create().WithId("Test").WithStatus(status).Build();
        var context = new DecisionEngineContext(
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
        var result = new TerminalStatusDecisionRule().Execute(context, engineContext => DecisionEngineResult.C02);

        // Assert using FluentAssertions
        result.Code.Should().Be(expectedDecisionCode);
        result.FurtherDetail.Should().Be(expectedInternalCode);
    }
}
