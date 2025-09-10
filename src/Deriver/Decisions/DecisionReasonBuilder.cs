using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Gvms;
using Defra.TradeImportsDataApi.Domain.Ipaffs;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

public static class DecisionReasonBuilder
{
    public static readonly string IuuErrorMessage =
        "Clearance of the Customs Declaration has been withheld. Confirmation of the outcome of IUU catch certificate check (under Council Regulation 1005/2008) is required. To resolve this contact your local Port Health Authority (imports) or MMO (landings).";

    public static string PortHealthErrorMessage(string chedType, string chedNumbers) =>
        $"A Customs Declaration has been submitted however no matching {chedType}(s) have been submitted to Port Health for {chedType} number(s) {chedNumbers}.";

    public static string AnimalHealthErrorMessage(string chedType, string chedNumbers) =>
        $"A Customs Declaration has been submitted however no matching {chedType}(s) have been submitted to Animal Health for {chedType} number(s) {chedNumbers}.";

    public static string GmsErrorMessage(
        string? ucr,
        int? itemNumber,
        decimal? netMass,
        string? taricCode,
        string? goodsDescription
    ) =>
        $"A Customs Declaration with a GMS product has been selected for HMI inspection. If using PEACH then raise a GMS application. If using IPAFFS then create a CHEDPP and amend your licence to reference it. If a CHEDPP exists amend your licence to reference it. Failure to do so will delay your Customs release. The selected item was declared on a Customs Declaration with a Declaration UCR of {ucr} and Item Number {itemNumber} with a weight of {netMass} and is TARIC Code {taricCode} with a description of {goodsDescription}.";

    public static List<string> Build(
        ClearanceRequest clearanceRequest,
        Commodity item,
        DocumentDecisionResult maxDecisionResult,
        DocumentDecisionResult[] documentDecisions
    )
    {
        var reasons = new List<string>();

        HandleNoLinkedNotifications(item, maxDecisionResult, documentDecisions, reasons);
        HandleHmiGmsDecisionReason(clearanceRequest, item, maxDecisionResult, reasons);

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
        ClearanceRequest clearanceRequest,
        Commodity item,
        DocumentDecisionResult maxDecisionResult,
        List<string> reasons
    )
    {
        if (
            item.Documents != null
            && item.Documents!.Any()
            && maxDecisionResult.DecisionCode == DecisionCode.X00
            && maxDecisionResult.CheckCode == "H220"
        )
        {
            reasons.Add(
                GmsErrorMessage(
                    clearanceRequest.DeclarationUcr,
                    item.ItemNumber,
                    item.NetMass,
                    item.TaricCommodityCode,
                    item.GoodsDescription
                )
            );
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
