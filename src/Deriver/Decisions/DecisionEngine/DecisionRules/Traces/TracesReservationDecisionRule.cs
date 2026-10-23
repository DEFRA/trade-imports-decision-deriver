using System.Text.Json;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;
using Microsoft.Extensions.Options;
using Refit;
using Trade.Gateway.Api.Contract.Customs;
using TradeImportsQuantityMgmt.Client.Clients;
using TradeImportsQuantityMgmt.Contract;
using ChedReservationRequest = TradeImportsQuantityMgmt.Contract.ChedReservationRequest;
using ReservationCommodityItem = TradeImportsQuantityMgmt.Contract.ReservationCommodityItem;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces;

public sealed class TracesReservationDecisionRule(
    IQuantityManagementClient quantityManagementClient,
    IOptions<DecisionRulesOptions> options
) : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        var result = next(context);

        if (!result.Code.IsRelease() || context.Level3Succeeded != true)
        {
            context.Logger.LogInformation(
                "Skipping reservation for {Ched} : {Mrn}",
                context.Ched?.ExchangedDocument.Identifier,
                context.ClearanceRequest.MovementReferenceNumber
            );
            return result;
        }

        context.Logger.LogInformation(
            "Running reservation for {Ched} : {Mrn}",
            context.Ched?.ExchangedDocument.Identifier,
            context.ClearanceRequest.MovementReferenceNumber
        );

        var request = new ChedReservationRequest { Items = BuildReservationItems(context) };

        // usage of .GetAwaiter().GetResult(); is temp until we can refactor the decision engine to be async
        var response = quantityManagementClient
            .PutChedReservation(
                context.Ched?.ExchangedDocument.Identifier!,
                context.ClearanceRequest.MovementReferenceNumber,
                request,
                CancellationToken.None
            )
            .GetAwaiter()
            .GetResult();

        if (response.IsSuccessful)
            return new DecisionEngineResult(DecisionCode.C03, nameof(TracesReservationDecisionRule));

        var internalCode = DecisionInternalFurtherDetail.E40;
        if (response.Error is ApiException apiEx)
        {
            // Deserialize with default (case-sensitive) options rather than apiEx.GetContentAsAsync: Refit's
            // default options are case-insensitive, so "reason" binds to the read-only Reason property and is
            // dropped instead of landing in Extensions, which is where Reason reads it from.
            var problem = TryReadProblemDetails(apiEx.Content);

            switch (problem?.Reason)
            {
                case ReservationFailureReason.CnCodesMismatch:
                    internalCode = DecisionInternalFurtherDetail.E41;
                    break;
                case ReservationFailureReason.InappropriateStatus:
                    internalCode = DecisionInternalFurtherDetail.E42;
                    break;
                case ReservationFailureReason.QuantitiesInsufficient:
                    internalCode = DecisionInternalFurtherDetail.E43;
                    break;
                case ReservationFailureReason.WriteOffExists:
                    internalCode = DecisionInternalFurtherDetail.E44;
                    break;
                case ReservationFailureReason.LineNumbersMismatch:
                    internalCode = DecisionInternalFurtherDetail.E45;
                    break;
                case ReservationFailureReason.MeasurementUnitMismatch:
                    internalCode = DecisionInternalFurtherDetail.E46;
                    break;
                case ReservationFailureReason.QuantitiesCannotBeValidated:
                    internalCode = DecisionInternalFurtherDetail.E47;
                    break;
            }
        }

        switch (options.Value.Traces.Level4Mode)
        {
            case RuleMode.Live:
                return new DecisionEngineResult(
                    DecisionCode.X00,
                    nameof(TracesReservationDecisionRule),
                    internalCode,
                    DecisionResultMode.Active,
                    DecisionRuleLevel.Level4
                );
            default:
                result.AddResult(
                    new DecisionEngineResult(
                        DecisionCode.X00,
                        nameof(TracesReservationDecisionRule),
                        internalCode,
                        DecisionResultMode.Passive,
                        DecisionRuleLevel.Level4
                    )
                );
                return result;
        }
    }

    private static ChedReservationProblemDetails? TryReadProblemDetails(string? content)
    {
        if (string.IsNullOrEmpty(content))
            return null;

        try
        {
            return JsonSerializer.Deserialize<ChedReservationProblemDetails>(content);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static ReservationCommodityItem[] BuildReservationItems(DecisionEngineContext context)
    {
        var consignmentItems = context.Ched?.SpecifiedConsignment?.IncludedConsignmentItem ?? [];
        var mrnCommodities = context.ClearanceRequest.CustomsDeclaration.ClearanceRequest?.Commodities ?? [];
        var chedDocumentIdentifier = context.ImportDocument?.DocumentReference?.Value;

        var items = new List<ReservationCommodityItem>();

        foreach (var tradeLineItem in consignmentItems.SelectMany(item => item.IncludedTradeLineItem ?? []))
        {
            var classCode = tradeLineItem
                .ApplicableClassification?.Select(classification => classification.ClassCode?.Value)
                .FirstOrDefault(value => !string.IsNullOrEmpty(value));

            if (classCode == null)
            {
                continue;
            }

            var matchedCommodity = mrnCommodities.FirstOrDefault(mrn =>
                mrn.TaricCommodityCode?.StartsWith(classCode, StringComparison.OrdinalIgnoreCase) == true
                && mrn.Documents?.Any(document => document.DocumentReference?.Value == chedDocumentIdentifier) == true
            );

            var netWeight = matchedCommodity?.NetMass ?? matchedCommodity?.SupplementaryUnits;

            if (matchedCommodity == null || netWeight == null)
            {
                continue;
            }

            items.Add(
                new ReservationCommodityItem
                {
                    GoodsItemNumber = matchedCommodity.ItemNumber,
                    CertificateLineNumber = tradeLineItem.SequenceNumeric,
                    ClassCode = classCode,
                    NetWeightQuantity = netWeight,
                    NetWeightUnitOfMeasure = UniversalUnitOfMeasureType.KGM,
                }
            );
        }

        return items.ToArray();
    }
}
