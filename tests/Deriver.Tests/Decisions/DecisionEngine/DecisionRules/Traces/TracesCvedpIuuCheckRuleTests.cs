using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Iuu;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Trade.Gateway.Api.Contract.Certificate;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.DecisionEngine.DecisionRules.Traces;

public class TracesCvedpIuuCheckRuleTests
{
    private const string IuuDocumentCode = "C673";
    private const string IuuReference = "GBIUU12345";

    private readonly ITradeImportsDataApiClient _apiClient = Substitute.For<ITradeImportsDataApiClient>();
    private readonly TracesCvedpIuuCheckRule _rule;
    private readonly DecisionRuleDelegate _mockNext = Substitute.For<DecisionRuleDelegate>();

    public TracesCvedpIuuCheckRuleTests()
    {
        _rule = new TracesCvedpIuuCheckRule(_apiClient);
    }

    [Fact]
    public void Execute_WhenCheckCodeIsNotIuu_CallsNextDelegate()
    {
        var c = CreateContext(new CheckCode() { Value = "H222" }, CreateIuuCommodity());

        _rule.Execute(c, _mockNext);

        _mockNext.Received(1).Invoke(Arg.Any<DecisionEngineContext>());
        _apiClient.DidNotReceiveWithAnyArgs().GetIuuNotification(default!, default);
    }

    [Fact]
    public void Execute_WhenNoValidIuuDocumentReference_ReturnsX00_E76()
    {
        var c = CreateContext(new CheckCode() { Value = CheckCode.IuuCheckCode }, new Commodity());

        var result = _rule.Execute(c, _mockNext);

        result
            .Should()
            .BeEquivalentTo(
                new DecisionEngineResult(
                    DecisionCode.X00,
                    nameof(TracesCvedpIuuCheckRule),
                    DecisionInternalFurtherDetail.E76
                )
            );
        _apiClient.DidNotReceiveWithAnyArgs().GetIuuNotification(default!, default);
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionEngineContext>());
    }

    [Theory]
    [InlineData(ControlAuthorityIuuOption.IUUOK, DecisionCode.C07)]
    [InlineData(ControlAuthorityIuuOption.IUUNotCompliant, DecisionCode.X00)]
    [InlineData(ControlAuthorityIuuOption.IUUNA, DecisionCode.C08)]
    [InlineData(null, DecisionCode.H02, DecisionInternalFurtherDetail.E93)]
    public void Execute_WhenIuuCertificateFound_ReturnsDecisionFromCertificate(
        string? iuuOption,
        DecisionCode expectedCode,
        DecisionInternalFurtherDetail? expectedDetail = null
    )
    {
        var c = CreateContext(new CheckCode() { Value = CheckCode.IuuCheckCode }, CreateIuuCommodity());

        _apiClient
            .GetIuuNotification(IuuReference, Arg.Any<CancellationToken>())
            .Returns(
                new IuuNotificationResponse(
                    new IuuNotification
                    {
                        Reference = IuuReference,
                        IuuOption = iuuOption!,
                        LastUpdated = DateTime.UtcNow,
                    },
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    "etag"
                )
            );

        var result = _rule.Execute(c, _mockNext);

        result
            .Should()
            .BeEquivalentTo(new DecisionEngineResult(expectedCode, nameof(TracesCvedpIuuCheckRule), expectedDetail));
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionEngineContext>());
    }

    [Fact]
    public void Execute_WhenIuuCertificateNotFound_ReturnsX00_E76()
    {
        var c = CreateContext(new CheckCode() { Value = CheckCode.IuuCheckCode }, CreateIuuCommodity());

        var result = _rule.Execute(c, _mockNext);

        result
            .Should()
            .BeEquivalentTo(
                new DecisionEngineResult(
                    DecisionCode.X00,
                    nameof(TracesCvedpIuuCheckRule),
                    DecisionInternalFurtherDetail.E76
                )
            );
        _apiClient.Received(1).GetIuuNotification(IuuReference, Arg.Any<CancellationToken>());
        _mockNext.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<DecisionEngineContext>());
    }

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

    private static DecisionEngineContext CreateContext(CheckCode checkCode, Commodity commodity)
    {
        var ched = new DefraUNVTDCHEDProfile()
        {
            ExchangedDocument = new ExchangedDocument()
            {
                DocumentStatusCode = TracesNotificationStatus.Validated,
                Identifier = "test",
            },
            SpecifiedConsignment = new Consignment(),
        };

        return new DecisionEngineContext(
            new DecisionContext([], [], [ched]),
            new DecisionRulesOptions(),
            null!,
            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
            commodity,
            checkCode,
            new ImportDocument(),
            ched
        )
        {
            Logger = NullLogger.Instance,
        };
    }
}
