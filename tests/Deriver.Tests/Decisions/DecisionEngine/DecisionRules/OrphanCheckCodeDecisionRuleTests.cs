using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.DecisionEngine.DecisionRules;

public class OrphanCheckCodeDecisionRuleTests
{
    private readonly OrphanCheckCodeDecisionRule _rule = new();
    private readonly DecisionRuleDelegate _mockNext = Substitute.For<DecisionRuleDelegate>();

    // ---------- ImportDocument not null calls next ----------
    [Fact]
    public void Execute_WhenImportDocumentIsNotNull_CallsNextDelegate()
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

        _rule.Execute(c, _mockNext);

        _mockNext.Received(1).Invoke(Arg.Any<DecisionEngineContext>());
    }

    // ---------- H220 checks with and without H219 ----------
    [Theory]
    [InlineData(true, DecisionInternalFurtherDetail.E82)] // Has H219
    [InlineData(false, DecisionInternalFurtherDetail.E87)] // Does not have H219
    public void Execute_WhenCheckCodeIsH220_ReturnsExpectedDecision(
        bool hasH219,
        DecisionInternalFurtherDetail expectedDetail
    )
    {
        var notification = DecisionImportPreNotificationBuilder.Create().WithId("Test").Build();
        var c = new DecisionEngineContext(
            new DecisionContext([notification], []),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity()
            {
                Checks = hasH219
                    ? [new CommodityCheck { CheckCode = "H219" }]
                    : [new CommodityCheck { CheckCode = "H220" }],
            },
            new CheckCode() { Value = "H220" },
            null
        )
        {
            Logger = NullLogger.Instance,
        };

        var expectedResult = DecisionEngineResult.Create(DecisionCode.X00, expectedDetail);

        var result = _rule.Execute(c, _mockNext);

        result.Should().BeEquivalentTo(expectedResult);
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionEngineContext>());
    }

    // ---------- CheckCode not H220 ----------
    [Theory]
    [InlineData("H221", DecisionInternalFurtherDetail.E83)]
    [InlineData("H222", DecisionInternalFurtherDetail.E83)]
    public void Execute_WhenCheckCodeIsNotH220_ReturnsE83(
        string checkCodeValue,
        DecisionInternalFurtherDetail expectedDetail
    )
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
            new CheckCode() { Value = checkCodeValue },
            null
        )
        {
            Logger = NullLogger.Instance,
        };

        var expectedResult = DecisionEngineResult.Create(DecisionCode.X00, expectedDetail);

        var result = _rule.Execute(c, _mockNext);

        result.Should().BeEquivalentTo(expectedResult);
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionEngineContext>());
    }
}
