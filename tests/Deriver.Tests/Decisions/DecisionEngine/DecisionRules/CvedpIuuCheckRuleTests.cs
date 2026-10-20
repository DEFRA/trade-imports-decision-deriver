using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Iuu;
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
    private const string IuuDocumentCode = "C673";
    private const string IuuReference = "GBIUU12345";

    private readonly ITradeImportsDataApiClient _apiClient = Substitute.For<ITradeImportsDataApiClient>();
    private readonly CvedpIuuCheckRule _rule;
    private readonly DecisionRuleDelegate _mockNext = Substitute.For<DecisionRuleDelegate>();

    public CvedpIuuCheckRuleTests()
    {
        _rule = new CvedpIuuCheckRule(_apiClient);
    }

    // ---------- Non-IUU checks call next ----------
    [Fact]
    public void Execute_WhenCheckCodeIsNotIuu_CallsNextDelegate()
    {
        var notification = DecisionImportPreNotificationBuilder.Create().WithId("Test").Build();
        var c = CreateContext(notification, new CheckCode() { Value = "H219" }, CreateIuuCommodity());

        _rule.Execute(c, _mockNext);

        _mockNext.Received(1).Invoke(Arg.Any<DecisionEngineContext>());
    }

    // ---------- IUU info on the notification (IuuCheckRequired = true and IuuOption set) ----------
    [Theory]
    [InlineData(ControlAuthorityIuuOption.IUUOK, DecisionCode.C07)]
    [InlineData(ControlAuthorityIuuOption.IUUNotCompliant, DecisionCode.X00)]
    [InlineData(ControlAuthorityIuuOption.IUUNA, DecisionCode.C08)]
    [InlineData("Unknown", DecisionCode.H02, DecisionInternalFurtherDetail.E93)]
    public void Execute_WhenNotificationHasIuuInfo_UsesNotificationAndDoesNotLoadCertificate(
        string iuuOption,
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
        var c = CreateContext(notification, new CheckCode() { Value = CheckCode.IuuCheckCode }, new Commodity());

        var result = _rule.Execute(c, _mockNext);

        result
            .Should()
            .BeEquivalentTo(new DecisionEngineResult(expectedCode, nameof(CvedpIuuCheckRule), expectedDetail));
        _apiClient.DidNotReceiveWithAnyArgs().GetIuuNotification(default!, default);
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionEngineContext>());
    }

    // ---------- No IUU info on the notification, IUU certificate is loaded ----------
    [Theory]
    [InlineData(false, ControlAuthorityIuuOption.IUUNA)]
    [InlineData(true, null)]
    [InlineData(null, null)]
    public void Execute_WhenNotificationHasNoIuuInfo_UsesIuuCertificate(bool? iuuCheckRequired, string? iuuOption)
    {
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithIuuCheckRequired(iuuCheckRequired)
            .WithIuuOption(iuuOption)
            .Build();
        var c = CreateContext(notification, new CheckCode() { Value = CheckCode.IuuCheckCode }, CreateIuuCommodity());

        _apiClient
            .GetIuuNotification(IuuReference, Arg.Any<CancellationToken>())
            .Returns(CreateIuuNotificationResponse(ControlAuthorityIuuOption.IUUOK));

        var result = _rule.Execute(c, _mockNext);

        result.Should().BeEquivalentTo(new DecisionEngineResult(DecisionCode.C07, nameof(CvedpIuuCheckRule)));
        _apiClient.Received(1).GetIuuNotification(IuuReference, Arg.Any<CancellationToken>());
    }

    // ---------- No IUU info on the notification, no valid IUU document reference ----------
    [Fact]
    public void Execute_WhenIuuCheckRequiredIsFalse_AndNoValidIuuDocumentReference_ReturnsX00_E76()
    {
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithIuuCheckRequired(false)
            .Build();
        var c = CreateContext(notification, new CheckCode() { Value = CheckCode.IuuCheckCode }, new Commodity());

        var result = _rule.Execute(c, _mockNext);

        result
            .Should()
            .BeEquivalentTo(
                new DecisionEngineResult(DecisionCode.X00, nameof(CvedpIuuCheckRule), DecisionInternalFurtherDetail.E76)
            );
        _apiClient.DidNotReceiveWithAnyArgs().GetIuuNotification(default!, default);
    }

    // ---------- No IUU info on the notification, IUU certificate found ----------
    [Theory]
    [InlineData(ControlAuthorityIuuOption.IUUOK, DecisionCode.C07)]
    [InlineData(ControlAuthorityIuuOption.IUUNotCompliant, DecisionCode.X00)]
    [InlineData(ControlAuthorityIuuOption.IUUNA, DecisionCode.C08)]
    [InlineData(null, DecisionCode.H02, DecisionInternalFurtherDetail.E93)]
    public void Execute_WhenIuuCheckRequiredIsFalse_AndIuuCertificateFound_ReturnsDecisionFromCertificate(
        string? iuuOption,
        DecisionCode expectedCode,
        DecisionInternalFurtherDetail? expectedDetail = null
    )
    {
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithIuuCheckRequired(false)
            .Build();
        var c = CreateContext(notification, new CheckCode() { Value = CheckCode.IuuCheckCode }, CreateIuuCommodity());

        _apiClient
            .GetIuuNotification(IuuReference, Arg.Any<CancellationToken>())
            .Returns(CreateIuuNotificationResponse(iuuOption));

        var result = _rule.Execute(c, _mockNext);

        result
            .Should()
            .BeEquivalentTo(new DecisionEngineResult(expectedCode, nameof(CvedpIuuCheckRule), expectedDetail));
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionEngineContext>());
    }

    // ---------- No IUU info on the notification, IUU certificate not found ----------
    [Fact]
    public void Execute_WhenIuuCheckRequiredIsFalse_AndIuuCertificateNotFound_ReturnsX00_E76()
    {
        var notification = DecisionImportPreNotificationBuilder
            .Create()
            .WithId("Test")
            .WithIuuCheckRequired(false)
            .Build();
        var c = CreateContext(notification, new CheckCode() { Value = CheckCode.IuuCheckCode }, CreateIuuCommodity());

        var result = _rule.Execute(c, _mockNext);

        result
            .Should()
            .BeEquivalentTo(
                new DecisionEngineResult(DecisionCode.X00, nameof(CvedpIuuCheckRule), DecisionInternalFurtherDetail.E76)
            );
        _apiClient.Received(1).GetIuuNotification(IuuReference, Arg.Any<CancellationToken>());
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionEngineContext>());
    }

    private static IuuNotificationResponse CreateIuuNotificationResponse(string? iuuOption) =>
        new(
            new IuuNotification
            {
                Reference = IuuReference,
                IuuOption = iuuOption!,
                LastUpdated = DateTime.UtcNow,
            },
            DateTime.UtcNow,
            DateTime.UtcNow,
            "etag"
        );

    private static Commodity CreateIuuCommodity() =>
        new()
        {
            Documents =
            [
                new ImportDocument
                {
                    DocumentCode = IuuDocumentCode,
                    DocumentReference = new ImportDocumentReference(IuuReference),
                },
            ],
        };

    private static DecisionEngineContext CreateContext(
        DecisionImportPreNotification notification,
        CheckCode checkCode,
        Commodity commodity
    ) =>
        new(
            new DecisionContext([notification], [], []),
            new DecisionRulesOptions(),
            notification,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            commodity,
            checkCode,
            new ImportDocument(),
            null
        )
        {
            Logger = NullLogger.Instance,
        };
}
