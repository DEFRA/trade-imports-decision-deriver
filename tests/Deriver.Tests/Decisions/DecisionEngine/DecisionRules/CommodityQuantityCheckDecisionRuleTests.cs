using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.DecisionEngine.DecisionRules;

public class CommodityQuantityCheckDecisionRuleTests
{
    private readonly DecisionRuleDelegate _mockNext = Substitute.For<DecisionRuleDelegate>();

    ////private readonly ILogger _mockLogger = Substitute.For<ILogger>();

    // Initialize the rule, mock context, mock next delegate, and logger
    //mode netmass

    [Theory]
    [InlineData(RuleMode.Live, 1, 1, DecisionCode.C02, null, DecisionResultMode.Active)]
    [InlineData(RuleMode.Live, 2, 1, DecisionCode.X00, DecisionInternalFurtherDetail.E30, DecisionResultMode.Active)]
    [InlineData(RuleMode.Live, 2, 3, DecisionCode.C02, null, DecisionResultMode.Active)]
    [InlineData(RuleMode.DryRun, 1, 1, DecisionCode.C02, null, DecisionResultMode.Active)]
    [InlineData(RuleMode.DryRun, 2, 1, DecisionCode.X00, DecisionInternalFurtherDetail.E30, DecisionResultMode.Passive)]
    [InlineData(RuleMode.DryRun, 2, 3, DecisionCode.C02, null, DecisionResultMode.Active)]
    public void NetMass_Tests(
        RuleMode ruleMode,
        decimal notificationWeight,
        decimal clearanceRequestWeight,
        DecisionCode expectedDecisionCode,
        DecisionInternalFurtherDetail? expectedDecisionInternalFurtherDetail,
        DecisionResultMode expectedDecisionResultMode
    )
    {
        // Arrange
        var rule = new CommodityQuantityCheckDecisionRule(
            Options.Create(new DecisionRulesOptions() { Level3Mode = ruleMode })
        );
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.Submitted)
            .WithInspectionRequired("Other")
            .AddCommodity(c => c.WithWeight(notificationWeight).WithCommodityCode("123"))
            .Build();

        _mockNext(Arg.Any<DecisionEngineContext>())
            .Returns(
                new DecisionEngineResult(
                    DecisionCode.C02,
                    nameof(CommodityQuantityCheckDecisionRule),
                    Level: DecisionRuleLevel.Level3
                )
            );

        var c = new DecisionEngineContext(
            new DecisionContext([notification], []),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity() { NetMass = clearanceRequestWeight, TaricCommodityCode = "123" },
            new CheckCode() { Value = "H221" },
            new ImportDocument()
        )
        {
            Logger = NullLogger.Instance,
        };

        // Act
        var returnedResult = rule.Execute(c, _mockNext);

        // Asser
        var resultToAssert =
            ruleMode == RuleMode.DryRun && returnedResult.PassiveResults is not null
                ? returnedResult.PassiveResults![0]
                : returnedResult;
        resultToAssert
            .Should()
            .BeEquivalentTo(
                new DecisionEngineResult(
                    expectedDecisionCode,
                    nameof(CommodityQuantityCheckDecisionRule),
                    expectedDecisionInternalFurtherDetail,
                    expectedDecisionResultMode,
                    DecisionRuleLevel.Level3
                )
            );
    }

    [Theory]
    [InlineData(RuleMode.Live, 1, 1, DecisionCode.C02, null, DecisionResultMode.Active)]
    [InlineData(RuleMode.Live, 2, 1, DecisionCode.X00, DecisionInternalFurtherDetail.E31, DecisionResultMode.Active)]
    [InlineData(RuleMode.Live, 2, 3, DecisionCode.C02, null, DecisionResultMode.Active)]
    [InlineData(RuleMode.DryRun, 1, 1, DecisionCode.C02, null, DecisionResultMode.Active)]
    [InlineData(RuleMode.DryRun, 2, 1, DecisionCode.X00, DecisionInternalFurtherDetail.E31, DecisionResultMode.Passive)]
    [InlineData(RuleMode.DryRun, 2, 3, DecisionCode.C02, null, DecisionResultMode.Active)]
    public void NetQuantity_Tests(
        RuleMode ruleMode,
        int notificationQuantity,
        int clearanceRequestQuantity,
        DecisionCode expectedDecisionCode,
        DecisionInternalFurtherDetail? expectedDecisionInternalFurtherDetail,
        DecisionResultMode expectedDecisionResultMode
    )
    {
        // Arrange
        var rule = new CommodityQuantityCheckDecisionRule(
            Options.Create(new DecisionRulesOptions() { Level3Mode = ruleMode })
        );
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.Submitted)
            .WithInspectionRequired("Other")
            .AddCommodity(c => c.WithQuantity(notificationQuantity).WithCommodityCode("123"))
            .Build();

        _mockNext(Arg.Any<DecisionEngineContext>())
            .Returns(
                new DecisionEngineResult(
                    DecisionCode.C02,
                    nameof(CommodityQuantityCheckDecisionRule),
                    Level: DecisionRuleLevel.Level3
                )
            );

        var c = new DecisionEngineContext(
            new DecisionContext([notification], []),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity() { SupplementaryUnits = clearanceRequestQuantity, TaricCommodityCode = "123" },
            new CheckCode() { Value = "H221" },
            new ImportDocument()
        )
        {
            Logger = NullLogger.Instance,
        };

        // Act
        var returnedResult = rule.Execute(c, _mockNext);

        // Asser
        var resultToAssert =
            ruleMode == RuleMode.DryRun && returnedResult.PassiveResults is not null
                ? returnedResult.PassiveResults![0]
                : returnedResult;
        resultToAssert
            .Should()
            .BeEquivalentTo(
                new DecisionEngineResult(
                    expectedDecisionCode,
                    nameof(CommodityQuantityCheckDecisionRule),
                    expectedDecisionInternalFurtherDetail,
                    expectedDecisionResultMode,
                    DecisionRuleLevel.Level3
                )
            );
    }

    [Fact]
    public void Execute_WhenResultCodeIsNotReleaseOrHold_ReturnsResultFromNextDelegate()
    {
        // Arrange
        var rule = new CommodityQuantityCheckDecisionRule(
            Options.Create(new DecisionRulesOptions() { Level3Mode = RuleMode.DryRun })
        );

        var result = new DecisionEngineResult(
            DecisionCode.X00,
            nameof(CommodityQuantityCheckDecisionRule),
            DecisionInternalFurtherDetail.E99
        );
        _mockNext(Arg.Any<DecisionEngineContext>()).Returns(result);

        var c = new DecisionEngineContext(
            new DecisionContext([], []),
            null!,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity(),
            new CheckCode() { Value = "H221" },
            new ImportDocument()
        )
        {
            Logger = NullLogger.Instance,
        };

        // Act
        var returnedResult = rule.Execute(c, _mockNext);

        // Assert
        returnedResult.Should().BeEquivalentTo(result);
    }
}
