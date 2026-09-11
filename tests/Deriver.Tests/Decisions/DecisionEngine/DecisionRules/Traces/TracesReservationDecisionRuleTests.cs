using System.Net;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Refit;
using Trade.Gateway.Api.Contract.Certificate;
using TradeImportsQuantityMgmt.Client.Clients;
using TradeImportsQuantityMgmt.Contract;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.DecisionEngine.DecisionRules.Traces;

public class TracesReservationDecisionRuleTests
{
    private readonly IQuantityManagementClient _quantityManagementClient = Substitute.For<IQuantityManagementClient>();
    private readonly DecisionRuleDelegate _mockNext = Substitute.For<DecisionRuleDelegate>();

    [Fact]
    public void Execute_WhenNextResultIsNotReleaseOrHold_ReturnsNextResultWithoutReserving()
    {
        var nextResult = StubNext(DecisionCode.N01);
        var context = CreateContext();

        var result = CreateRule().Execute(context, _mockNext);

        result.Should().Be(nextResult);
        _quantityManagementClient
            .DidNotReceive()
            .PutChedReservation(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ChedReservationRequest>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public void Execute_WhenLevel3Failed_ReturnsNextResultWithoutReserving()
    {
        var nextResult = StubNext(DecisionCode.C02);
        var context = CreateContext(level3Succeeded: false);

        var result = CreateRule().Execute(context, _mockNext);

        result.Should().Be(nextResult);
        _quantityManagementClient
            .DidNotReceive()
            .PutChedReservation(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ChedReservationRequest>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public void Execute_WhenLevel3SucceededIsNull_ReturnsNextResultWithoutReserving()
    {
        // Level3Succeeded is only ever set to true/false once TracesCommodityQuantityCheckDecisionRule
        // actually runs its validation - if an earlier gate (e.g. Level2) short-circuited the chain first,
        // it stays null. That must be treated the same as an explicit failure, not as a pass.
        var nextResult = StubNext(DecisionCode.C02);
        var context = CreateContext(level3Succeeded: null);

        var result = CreateRule().Execute(context, _mockNext);

        result.Should().Be(nextResult);
        _quantityManagementClient
            .DidNotReceive()
            .PutChedReservation(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ChedReservationRequest>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public void Execute_WhenReservationSucceeds_ReturnsC03()
    {
        StubNext(DecisionCode.C02);
        var context = CreateContext();
        StubResponse(HttpStatusCode.OK, new ChedDeclarationReservation { Reserved = [], Consumed = [] });

        var result = CreateRule().Execute(context, _mockNext);

        result.Code.Should().Be(DecisionCode.C03);
    }

    [Fact]
    public void Execute_WhenReservationFailsAndLevel3ModeIsLive_ReturnsActiveX00Level4()
    {
        StubNext(DecisionCode.C02);
        var context = CreateContext();
        StubResponse(HttpStatusCode.BadRequest, null);

        var result = CreateRule(RuleMode.Live).Execute(context, _mockNext);

        result
            .Should()
            .Be(
                new DecisionEngineResult(
                    DecisionCode.X00,
                    nameof(TracesReservationDecisionRule),
                    DecisionInternalFurtherDetail.E99,
                    DecisionResultMode.Active,
                    DecisionRuleLevel.Level4
                )
            );
    }

    [Fact]
    public void Execute_WhenReservationFailsAndLevel3ModeIsDryRun_AddsPassiveResultToNextResult()
    {
        var nextResult = StubNext(DecisionCode.C02);
        var context = CreateContext();
        StubResponse(HttpStatusCode.BadRequest, null);

        var result = CreateRule(RuleMode.DryRun).Execute(context, _mockNext);

        result.Should().BeSameAs(nextResult);
        result.Code.Should().Be(DecisionCode.C02);
        result
            .PassiveResults?[0].Should()
            .Be(
                new DecisionEngineResult(
                    DecisionCode.X00,
                    nameof(TracesReservationDecisionRule),
                    DecisionInternalFurtherDetail.E99,
                    DecisionResultMode.Passive,
                    DecisionRuleLevel.Level4
                )
            );
    }

    [Fact]
    public void Execute_PutsReservationAgainstChedAndMrn()
    {
        StubNext(DecisionCode.C02);
        var context = CreateContext();
        StubResponse(HttpStatusCode.OK, new ChedDeclarationReservation { Reserved = [], Consumed = [] });

        CreateRule().Execute(context, _mockNext);

        _quantityManagementClient
            .Received(1)
            .PutChedReservation(
                "CHEDP.GB.2025.1234567",
                "mrn",
                Arg.Any<ChedReservationRequest>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public void Execute_WhenNetMassPresent_BuildsItemFromWeight()
    {
        StubNext(DecisionCode.C02);
        var context = CreateContext(netMass: 100, supplementaryUnits: 999);
        var capturedRequest = StubResponseAndCapture(HttpStatusCode.OK);

        CreateRule().Execute(context, _mockNext);

        capturedRequest.Request.Should().NotBeNull();
        capturedRequest.Request!.Items.Should().ContainSingle();
        var item = capturedRequest.Request.Items[0];
        item.GoodsItemNumber.Should().Be(1);
        item.CertificateLineNumber.Should().Be(1);
        item.ClassCode.Should().Be("0207146000");
        item.NetWeightQuantity.Should().Be(100);
        item.NetWeightUnitOfMeasure.Should().Be(UniversalUnitOfMeasureType.KGM);
    }

    [Fact]
    public void Execute_WhenNetMassAbsent_FallsBackToSupplementaryUnits()
    {
        StubNext(DecisionCode.C02);
        var context = CreateContext(netMass: null, supplementaryUnits: 42);
        var capturedRequest = StubResponseAndCapture(HttpStatusCode.OK);

        CreateRule().Execute(context, _mockNext);

        capturedRequest.Request.Should().NotBeNull();
        var item = capturedRequest.Request!.Items.Should().ContainSingle().Subject;
        item.NetWeightQuantity.Should().Be(42);
        item.NetWeightUnitOfMeasure.Should().Be(UniversalUnitOfMeasureType.KGM);
    }

    [Fact]
    public void Execute_WhenNoWeightOrSupplementaryUnits_ExcludesItem()
    {
        StubNext(DecisionCode.C02);
        var context = CreateContext(netMass: null, supplementaryUnits: null);
        var capturedRequest = StubResponseAndCapture(HttpStatusCode.OK);

        CreateRule().Execute(context, _mockNext);

        capturedRequest.Request.Should().NotBeNull();
        capturedRequest.Request!.Items.Should().BeEmpty();
    }

    [Fact]
    public void Execute_WhenCommodityCodeDoesNotMatch_ExcludesItem()
    {
        StubNext(DecisionCode.C02);
        var context = CreateContext(taricCommodityCode: "9999999999");
        var capturedRequest = StubResponseAndCapture(HttpStatusCode.OK);

        CreateRule().Execute(context, _mockNext);

        capturedRequest.Request.Should().NotBeNull();
        capturedRequest.Request!.Items.Should().BeEmpty();
    }

    [Fact]
    public void Execute_WhenDocumentReferenceDoesNotMatch_ExcludesItem()
    {
        StubNext(DecisionCode.C02);
        var context = CreateContext(documentReference: "9999999");
        var capturedRequest = StubResponseAndCapture(HttpStatusCode.OK);

        CreateRule().Execute(context, _mockNext);

        capturedRequest.Request.Should().NotBeNull();
        capturedRequest.Request!.Items.Should().BeEmpty();
    }

    [Fact]
    public void Execute_WhenNoConsignmentItemsOnChed_SendsEmptyItems()
    {
        StubNext(DecisionCode.C02);
        var context = CreateContext(includeConsignmentItem: false);
        var capturedRequest = StubResponseAndCapture(HttpStatusCode.OK);

        CreateRule().Execute(context, _mockNext);

        capturedRequest.Request.Should().NotBeNull();
        capturedRequest.Request!.Items.Should().BeEmpty();
    }

    [Fact]
    public void Execute_WhenChedHasMultipleConsignmentItems_BuildsAnItemForEachMatch()
    {
        StubNext(DecisionCode.C02);

        var chedIdentifier = "CHEDP.GB.2025.1234567";
        var ched = new DefraUNVTDCHEDProfile()
        {
            ExchangedDocument = new ExchangedDocument() { Identifier = chedIdentifier },
            SpecifiedConsignment = new Consignment()
            {
                IncludedConsignmentItem =
                [
                    new ConsignmentItem()
                    {
                        IncludedTradeLineItem =
                        [
                            new TradeLineItem()
                            {
                                SequenceNumeric = 1,
                                ApplicableClassification =
                                [
                                    new ApplicableClassification() { ClassCode = new CodedValue { Value = "0101" } },
                                ],
                            },
                        ],
                    },
                    new ConsignmentItem()
                    {
                        IncludedTradeLineItem =
                        [
                            new TradeLineItem()
                            {
                                SequenceNumeric = 2,
                                ApplicableClassification =
                                [
                                    new ApplicableClassification() { ClassCode = new CodedValue { Value = "0202" } },
                                ],
                            },
                        ],
                    },
                ],
            },
        };

        var document = new ImportDocument()
        {
            DocumentCode = "C640",
            DocumentReference = new ImportDocumentReference("1234567"),
        };

        var customsDeclaration = new CustomsDeclarationWrapper(
            "mrn",
            new CustomsDeclaration()
            {
                ClearanceRequest = new ClearanceRequest()
                {
                    Commodities =
                    [
                        new Commodity()
                        {
                            ItemNumber = 1,
                            TaricCommodityCode = "0101010000",
                            NetMass = 10,
                            Documents = [document],
                        },
                        new Commodity()
                        {
                            ItemNumber = 2,
                            TaricCommodityCode = "0202020000",
                            NetMass = 20,
                            Documents = [document],
                        },
                    ],
                },
            }
        );

        var context = new DecisionEngineContext(
            new DecisionContext([], [customsDeclaration], [ched]),
            new DecisionRulesOptions(),
            null!,
            customsDeclaration,
            customsDeclaration.CustomsDeclaration.ClearanceRequest!.Commodities![0],
            new CheckCode() { Value = "H222" },
            document,
            ched
        )
        {
            Logger = NullLogger.Instance,
            Level3Succeeded = true,
        };

        var capturedRequest = StubResponseAndCapture(HttpStatusCode.OK);

        CreateRule().Execute(context, _mockNext);

        capturedRequest.Request.Should().NotBeNull();
        capturedRequest.Request!.Items.Should().HaveCount(2);
        capturedRequest.Request.Items.Should().Contain(i => i.GoodsItemNumber == 1 && i.NetWeightQuantity == 10);
        capturedRequest.Request.Items.Should().Contain(i => i.GoodsItemNumber == 2 && i.NetWeightQuantity == 20);
    }

    private TracesReservationDecisionRule CreateRule(RuleMode level4Mode = RuleMode.DryRun)
    {
        return new TracesReservationDecisionRule(
            _quantityManagementClient,
            Options.Create(new DecisionRulesOptions() { Level4Mode = level4Mode })
        );
    }

    private DecisionEngineResult StubNext(DecisionCode code)
    {
        var result = new DecisionEngineResult(code, "Next");
        _mockNext.Invoke(Arg.Any<DecisionEngineContext>()).Returns(result);
        return result;
    }

    private void StubResponse(HttpStatusCode statusCode, ChedDeclarationReservation? content)
    {
        _quantityManagementClient
            .PutChedReservation(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ChedReservationRequest>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new ApiResponse<ChedDeclarationReservation>(new HttpResponseMessage(statusCode), content!, null!, null!)
            );
    }

    private CapturedRequest StubResponseAndCapture(HttpStatusCode statusCode)
    {
        var captured = new CapturedRequest();
        _quantityManagementClient
            .PutChedReservation(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Do<ChedReservationRequest>(r => captured.Request = r),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new ApiResponse<ChedDeclarationReservation>(
                    new HttpResponseMessage(statusCode),
                    new ChedDeclarationReservation { Reserved = [], Consumed = [] },
                    null!,
                    null!
                )
            );
        return captured;
    }

    private sealed class CapturedRequest
    {
        public ChedReservationRequest? Request { get; set; }
    }

    private static DecisionEngineContext CreateContext(
        decimal? netMass = 100,
        decimal? supplementaryUnits = null,
        string taricCommodityCode = "0207146000",
        string documentReference = "1234567",
        bool includeConsignmentItem = true,
        bool? level3Succeeded = true
    )
    {
        const string chedIdentifier = "CHEDP.GB.2025.1234567";

        var ched = new DefraUNVTDCHEDProfile()
        {
            ExchangedDocument = new ExchangedDocument() { Identifier = chedIdentifier },
            SpecifiedConsignment = new Consignment()
            {
                IncludedConsignmentItem = includeConsignmentItem
                    ?
                    [
                        new ConsignmentItem()
                        {
                            IncludedTradeLineItem =
                            [
                                new TradeLineItem()
                                {
                                    SequenceNumeric = 1,
                                    ApplicableClassification =
                                    [
                                        new ApplicableClassification()
                                        {
                                            ClassCode = new CodedValue { Value = "0207146000" },
                                        },
                                    ],
                                },
                            ],
                        },
                    ]
                    : [],
            },
        };

        var document = new ImportDocument()
        {
            DocumentCode = "C640",
            DocumentReference = new ImportDocumentReference("1234567"),
        };

        var customsDeclaration = new CustomsDeclarationWrapper(
            "mrn",
            new CustomsDeclaration()
            {
                ClearanceRequest = new ClearanceRequest()
                {
                    Commodities =
                    [
                        new Commodity()
                        {
                            ItemNumber = 1,
                            TaricCommodityCode = taricCommodityCode,
                            NetMass = netMass,
                            SupplementaryUnits = supplementaryUnits,
                            Documents =
                            [
                                new ImportDocument()
                                {
                                    DocumentCode = "C640",
                                    DocumentReference = new ImportDocumentReference(documentReference),
                                },
                            ],
                        },
                    ],
                },
            }
        );

        return new DecisionEngineContext(
            new DecisionContext([], [customsDeclaration], [ched]),
            new DecisionRulesOptions(),
            null!,
            customsDeclaration,
            customsDeclaration.CustomsDeclaration.ClearanceRequest!.Commodities![0],
            new CheckCode() { Value = "H222" },
            document,
            ched
        )
        {
            Logger = NullLogger.Instance,
            Level3Succeeded = level3Succeeded,
        };
    }
}
