using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.DecisionEngine.DecisionRules;

public class ChedppDecisionRuleTests
{
    private readonly ChedppDecisionRule _rule = new();
    private readonly DecisionRuleDelegate _mockNext = Substitute.For<DecisionRuleDelegate>();

    // ---------- Status theory ----------
    [Theory]
    [InlineData(ImportNotificationStatus.Submitted, DecisionCode.H02, null)]
    [InlineData(ImportNotificationStatus.InProgress, DecisionCode.H02, null)]
    [InlineData(ImportNotificationStatus.PartiallyRejected, DecisionCode.H01, DecisionInternalFurtherDetail.E74)]
    [InlineData("Unknown", DecisionCode.X00, DecisionInternalFurtherDetail.E99)]
    public void Execute_WhenStatus_ReturnsExpectedResult(
        string status,
        DecisionCode expectedCode,
        DecisionInternalFurtherDetail? expectedDetail
    )
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder.Create().WithId("Test").WithStatus(status).Build();

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

    // ---------- HMI checks ----------
    [Theory]
    [InlineData("Compliant", DecisionCode.C03, null)]
    [InlineData("Auto cleared", DecisionCode.C03, null)]
    [InlineData("To do", DecisionCode.H01, null)]
    [InlineData("Hold", DecisionCode.H01, null)]
    [InlineData("To be inspected", DecisionCode.H02, null)]
    [InlineData("Non compliant", DecisionCode.N01, null)]
    [InlineData("Not inspected", DecisionCode.C02, null)]
    [InlineData("Unknown", DecisionCode.X00, DecisionInternalFurtherDetail.E99)]
    public void Execute_WhenHmiCheckStatus_ReturnsExpectedDecision(
        string hmiStatus,
        DecisionCode expectedCode,
        DecisionInternalFurtherDetail? expectedDetail
    )
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.Validated)
            .AddCommodityCheck(c => c.WithType("HMI").WithStatus(hmiStatus))
            .Build();

        var c = new DecisionEngineContext(
            new DecisionContext([notification], []),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity(),
            new CheckCode() { Value = "H218" },
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

    // ---------- PHSI checks ----------
    [Theory]
    [InlineData("Compliant", "Hold", "Non compliant", DecisionCode.N01)] // Highest precedence
    [InlineData("To do", "Hold", "Compliant", DecisionCode.H01)]
    [InlineData("To be inspected", "To be inspected", "To be inspected", DecisionCode.H02)]
    public void Execute_WhenPhsiChecks_ReturnsHighestDecision(
        string documentStatus,
        string physicalStatus,
        string identityStatus,
        DecisionCode expectedCode
    )
    {
        // Arrange
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.Validated)
            .AddCommodityCheck(c => c.WithType("PHSI_DOCUMENT").WithStatus(documentStatus))
            .AddCommodityCheck(c => c.WithType("PHSI_PHYSICAL").WithStatus(physicalStatus))
            .AddCommodityCheck(c => c.WithType("PHSI_IDENTITY").WithStatus(identityStatus))
            .Build();

        var c = new DecisionEngineContext(
            new DecisionContext([notification], []),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity(),
            new CheckCode() { Value = "H219" },
            new ImportDocument()
        )
        {
            Logger = NullLogger.Instance,
        };

        var expectedResult = DecisionEngineResult.Create(expectedCode);

        // Act
        var result = _rule.Execute(c, _mockNext);

        // Assert
        result.Should().BeEquivalentTo(expectedResult);
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionEngineContext>());
    }

    // ---------- Missing HMI ----------
    [Fact]
    public void Execute_WhenNoHMICheck_ReturnsH01_E86()
    {
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.Validated)
            .Build();

        var c = new DecisionEngineContext(
            new DecisionContext([notification], []),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity(),
            new CheckCode() { Value = "H218" },
            new ImportDocument()
        )
        {
            Logger = NullLogger.Instance,
        };

        var expectedResult = DecisionEngineResult.Create(DecisionCode.H01, DecisionInternalFurtherDetail.E86);

        var result = _rule.Execute(c, _mockNext);

        result.Should().BeEquivalentTo(expectedResult);
    }

    // ---------- Missing PHSI ----------
    [Fact]
    public void Execute_WhenMissingPhsiChecks_ReturnsH01_E85()
    {
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithStatus(ImportNotificationStatus.Validated)
            .Build();

        var c = new DecisionEngineContext(
            new DecisionContext([notification], []),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            new Commodity(),
            new CheckCode() { Value = "H219" },
            new ImportDocument()
        )
        {
            Logger = NullLogger.Instance,
        };

        var expectedResult = DecisionEngineResult.Create(DecisionCode.H01, DecisionInternalFurtherDetail.E85);

        var result = _rule.Execute(c, _mockNext);

        result.Should().BeEquivalentTo(expectedResult);
    }
}
