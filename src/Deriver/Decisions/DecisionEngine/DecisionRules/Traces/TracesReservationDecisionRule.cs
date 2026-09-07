using TradeImportsQuantityMgmt.Client.Clients;
using TradeImportsQuantityMgmt.Contract;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces;

public sealed class TracesReservationDecisionRule(IQuantityManagementClient quantityManagementClient) : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
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

        return response.IsSuccessful
            ? new DecisionEngineResult(DecisionCode.C03, nameof(TracesReservationDecisionRule))
            : new DecisionEngineResult(
                DecisionCode.X00,
                nameof(TracesReservationDecisionRule),
                DecisionInternalFurtherDetail.E99
            );
    }

    private static ReservationCommodityItem[] BuildReservationItems(DecisionEngineContext context)
    {
        var consignmentItems = context.Ched?.SpecifiedConsignment?.IncludedConsignmentItem ?? [];
        var mrnCommodities = context.ClearanceRequest.CustomsDeclaration.ClearanceRequest?.Commodities ?? [];
        var chedDocumentIdentifier = context.ImportDocument?.GetDocumentReferenceIdentifier();

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
                && mrn.Documents?.Any(document => document.GetDocumentReferenceIdentifier() == chedDocumentIdentifier)
                    == true
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
