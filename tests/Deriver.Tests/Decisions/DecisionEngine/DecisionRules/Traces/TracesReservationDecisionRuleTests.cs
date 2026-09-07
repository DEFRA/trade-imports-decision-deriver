using System.Net;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Microsoft.Extensions.Logging.Abstractions;
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
    public void Execute_WhenReservationSucceeds_ReturnsC03()
    {
        var context = CreateContext();
        StubResponse(HttpStatusCode.OK, new ChedDeclarationReservation { Reserved = [], Consumed = [] });

        var result = new TracesReservationDecisionRule(_quantityManagementClient).Execute(context, _mockNext);

        result.Code.Should().Be(DecisionCode.C03);
    }

    [Fact]
    public void Execute_WhenReservationFails_ReturnsX00WithE99()
    {
        var context = CreateContext();
        StubResponse(HttpStatusCode.BadRequest, null);

        var result = new TracesReservationDecisionRule(_quantityManagementClient).Execute(context, _mockNext);

        result.Code.Should().Be(DecisionCode.X00);
        result.FurtherDetail.Should().Be(DecisionInternalFurtherDetail.E99);
    }

    [Fact]
    public void Execute_PutsReservationAgainstChedAndMrn()
    {
        var context = CreateContext();
        StubResponse(HttpStatusCode.OK, new ChedDeclarationReservation { Reserved = [], Consumed = [] });

        new TracesReservationDecisionRule(_quantityManagementClient).Execute(context, _mockNext);

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
        var context = CreateContext(netMass: 100, supplementaryUnits: 999);
        StubResponse(HttpStatusCode.OK, new ChedDeclarationReservation { Reserved = [], Consumed = [] });

        ChedReservationRequest? capturedRequest = null;
        _quantityManagementClient
            .PutChedReservation(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Do<ChedReservationRequest>(r => capturedRequest = r),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new ApiResponse<ChedDeclarationReservation>(
                    new HttpResponseMessage(HttpStatusCode.OK),
                    new ChedDeclarationReservation { Reserved = [], Consumed = [] },
                    null!,
                    null!
                )
            );

        new TracesReservationDecisionRule(_quantityManagementClient).Execute(context, _mockNext);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Items.Should().ContainSingle();
        var item = capturedRequest.Items[0];
        item.GoodsItemNumber.Should().Be(1);
        item.CertificateLineNumber.Should().Be(1);
        item.ClassCode.Should().Be("0207146000");
        item.NetWeightQuantity.Should().Be(100);
        item.NetWeightUnitOfMeasure.Should().Be(UniversalUnitOfMeasureType.KGM);
    }

    [Fact]
    public void Execute_WhenNetMassAbsent_FallsBackToSupplementaryUnits()
    {
        var context = CreateContext(netMass: null, supplementaryUnits: 42);

        ChedReservationRequest? capturedRequest = null;
        _quantityManagementClient
            .PutChedReservation(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Do<ChedReservationRequest>(r => capturedRequest = r),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new ApiResponse<ChedDeclarationReservation>(
                    new HttpResponseMessage(HttpStatusCode.OK),
                    new ChedDeclarationReservation { Reserved = [], Consumed = [] },
                    null!,
                    null!
                )
            );

        new TracesReservationDecisionRule(_quantityManagementClient).Execute(context, _mockNext);

        capturedRequest.Should().NotBeNull();
        var item = capturedRequest!.Items.Should().ContainSingle().Subject;
        item.NetWeightQuantity.Should().Be(42);
        item.NetWeightUnitOfMeasure.Should().Be(UniversalUnitOfMeasureType.KGM);
    }

    [Fact]
    public void Execute_WhenNoWeightOrSupplementaryUnits_ExcludesItem()
    {
        var context = CreateContext(netMass: null, supplementaryUnits: null);

        ChedReservationRequest? capturedRequest = null;
        _quantityManagementClient
            .PutChedReservation(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Do<ChedReservationRequest>(r => capturedRequest = r),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new ApiResponse<ChedDeclarationReservation>(
                    new HttpResponseMessage(HttpStatusCode.OK),
                    new ChedDeclarationReservation { Reserved = [], Consumed = [] },
                    null!,
                    null!
                )
            );

        new TracesReservationDecisionRule(_quantityManagementClient).Execute(context, _mockNext);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Items.Should().BeEmpty();
    }

    [Fact]
    public void Execute_WhenCommodityCodeDoesNotMatch_ExcludesItem()
    {
        var context = CreateContext(taricCommodityCode: "9999999999");

        ChedReservationRequest? capturedRequest = null;
        _quantityManagementClient
            .PutChedReservation(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Do<ChedReservationRequest>(r => capturedRequest = r),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new ApiResponse<ChedDeclarationReservation>(
                    new HttpResponseMessage(HttpStatusCode.OK),
                    new ChedDeclarationReservation { Reserved = [], Consumed = [] },
                    null!,
                    null!
                )
            );

        new TracesReservationDecisionRule(_quantityManagementClient).Execute(context, _mockNext);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Items.Should().BeEmpty();
    }

    [Fact]
    public void Execute_WhenDocumentReferenceDoesNotMatch_ExcludesItem()
    {
        var context = CreateContext(documentReference: "9999999");

        ChedReservationRequest? capturedRequest = null;
        _quantityManagementClient
            .PutChedReservation(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Do<ChedReservationRequest>(r => capturedRequest = r),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new ApiResponse<ChedDeclarationReservation>(
                    new HttpResponseMessage(HttpStatusCode.OK),
                    new ChedDeclarationReservation { Reserved = [], Consumed = [] },
                    null!,
                    null!
                )
            );

        new TracesReservationDecisionRule(_quantityManagementClient).Execute(context, _mockNext);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Items.Should().BeEmpty();
    }

    [Fact]
    public void Execute_WhenNoConsignmentItemsOnChed_SendsEmptyItems()
    {
        var context = CreateContext(includeConsignmentItem: false);

        ChedReservationRequest? capturedRequest = null;
        _quantityManagementClient
            .PutChedReservation(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Do<ChedReservationRequest>(r => capturedRequest = r),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new ApiResponse<ChedDeclarationReservation>(
                    new HttpResponseMessage(HttpStatusCode.OK),
                    new ChedDeclarationReservation { Reserved = [], Consumed = [] },
                    null!,
                    null!
                )
            );

        new TracesReservationDecisionRule(_quantityManagementClient).Execute(context, _mockNext);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Items.Should().BeEmpty();
    }

    [Fact]
    public void Execute_WhenChedHasMultipleConsignmentItems_BuildsAnItemForEachMatch()
    {
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
        };

        ChedReservationRequest? capturedRequest = null;
        _quantityManagementClient
            .PutChedReservation(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Do<ChedReservationRequest>(r => capturedRequest = r),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new ApiResponse<ChedDeclarationReservation>(
                    new HttpResponseMessage(HttpStatusCode.OK),
                    new ChedDeclarationReservation { Reserved = [], Consumed = [] },
                    null!,
                    null!
                )
            );

        new TracesReservationDecisionRule(_quantityManagementClient).Execute(context, _mockNext);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Items.Should().HaveCount(2);
        capturedRequest.Items.Should().Contain(i => i.GoodsItemNumber == 1 && i.NetWeightQuantity == 10);
        capturedRequest.Items.Should().Contain(i => i.GoodsItemNumber == 2 && i.NetWeightQuantity == 20);
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

    private static DecisionEngineContext CreateContext(
        decimal? netMass = 100,
        decimal? supplementaryUnits = null,
        string taricCommodityCode = "0207146000",
        string documentReference = "1234567",
        bool includeConsignmentItem = true
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
        };
    }
}
