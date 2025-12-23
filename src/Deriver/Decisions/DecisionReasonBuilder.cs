using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Gvms;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.Processors;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

public static class DecisionReasonBuilder
{
    public static readonly string IuuErrorMessage =
        "Clearance of the Customs Declaration has been withheld. Confirmation of the outcome of IUU catch certificate check (under Council Regulation 1005/2008) is required. To resolve this contact your local Port Health Authority (imports) or MMO (landings).";

    public static string PortHealthErrorMessage(string chedType, string chedNumbers) =>
        $"A Customs Declaration has been submitted however no matching {chedType}(s) have been submitted to Port Health for {chedType} number(s) {chedNumbers}.";

    public static string AnimalHealthErrorMessage(string chedType, string chedNumbers) =>
        $"A Customs Declaration has been submitted however no matching {chedType}(s) have been submitted to Animal Health for {chedType} number(s) {chedNumbers}.";

    public static string GmsErrorMessage(int? itemNumber, string? taricCode, string? goodsDescription) =>
        $"Item number {itemNumber} has been selected for HMI GMS inspection. In IPAFFS either create a new CHEDPP with this item or amend an existing CHEDPP to include this item. The item selected has a TARIC code of {taricCode} and a description of {goodsDescription}.";

    public static List<string> Build(
        ClearanceRequest clearanceRequest,
        Commodity item,
        DocumentDecisionResult maxDecisionResult,
        DocumentDecisionResult[] documentDecisions
    )
    {
        var reasons = new List<string>();

        HandleNoLinkedNotifications(item, maxDecisionResult, documentDecisions, reasons);
        HandleHmiGmsDecisionReason(item, maxDecisionResult, reasons);

        return reasons;
    }

    public static List<string> Build(
        ClearanceRequest clearanceRequest,
        Commodity item,
        CheckDecisionResult maxDecisionResult,
        CheckDecisionResult[] documentDecisions
    )
    {
        var reasons = new List<string>();

        HandleNoLinkedNotifications(item, maxDecisionResult, documentDecisions, reasons);
        HandleHmiGmsDecisionReason(item, maxDecisionResult, reasons);

        return reasons;
    }

    private static void HandleNoLinkedNotifications(
        Commodity item,
        DocumentDecisionResult maxDecisionResult,
        DocumentDecisionResult[] documentDecisions,
        List<string> reasons
    )
    {
        if (
            item.Documents != null
            && item.Documents.Any()
            && maxDecisionResult.DecisionCode == DecisionCode.X00
            && maxDecisionResult.CheckCode != "H220"
        )
        {
            if (maxDecisionResult.InternalDecisionCode == DecisionInternalFurtherDetail.E83)
            {
                return;
            }

            var chedType = MapToChedType(
                new ImportDocument()
                {
                    DocumentReference = new ImportDocumentReference(maxDecisionResult.DocumentReference),
                    DocumentCode = maxDecisionResult.DocumentCode,
                }
            );
            var chedNumbers = string.Join(
                ", ",
                documentDecisions
                    .Where(x => x.DecisionCode == DecisionCode.X00)
                    .Select(x => x.DocumentReference)
                    .Distinct()
            );

            switch (maxDecisionResult.DocumentCode)
            {
                case "C673":
                case "C641":
                    reasons.Add(IuuErrorMessage);
                    break;
                case "N853":
                    reasons.Add(
                        maxDecisionResult.CheckCode == "H224"
                            ? IuuErrorMessage
                            : PortHealthErrorMessage(chedType, chedNumbers)
                    );
                    break;
                case "C640":
                    reasons.Add(AnimalHealthErrorMessage(chedType, chedNumbers));
                    break;
                default:
                    reasons.Add(PortHealthErrorMessage(chedType, chedNumbers));
                    break;
            }
        }
    }

    private static void HandleHmiGmsDecisionReason(
        Commodity item,
        DocumentDecisionResult maxDecisionResult,
        List<string> reasons
    )
    {
        if (maxDecisionResult is { DecisionCode: DecisionCode.X00, CheckCode: "H220" })
        {
            reasons.Add(GmsErrorMessage(item.ItemNumber, item.TaricCommodityCode, item.GoodsDescription));
        }
    }

    private static string MapToChedType(ImportDocument? documentCode)
    {
        var ct = documentCode?.GetChedType();

        if (ct is null)
        {
            throw new ArgumentOutOfRangeException(nameof(documentCode), documentCode, null);
        }

        return ct;
    }
}
