using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Ipaffs;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

public static class DecisionReasonBuilder
{
    public static List<string> Build(Commodity item, DocumentDecisionResult maxDecisionResult)
    {
        var reasons = new List<string>();

        if (maxDecisionResult.DecisionReason != null)
        {
            reasons.Add(maxDecisionResult.DecisionReason);
        }

        HandleNoLinkedNotifications(item, maxDecisionResult, reasons);
        HandleHmiGmsDecisionReason(item, maxDecisionResult, reasons);

        return reasons;
    }

    private static void HandleNoLinkedNotifications(
        Commodity item,
        DocumentDecisionResult maxDecisionResult,
        List<string> reasons
    )
    {
        if (item.Documents != null && item.Documents.Any() && maxDecisionResult.DecisionCode == DecisionCode.X00)
        {
            var chedType = MapToChedType(item.Documents[0]);
            var chedNumbers = string.Join(", ", item.Documents.Select(x => x.DocumentReference?.Value));

            if (reasons.Count == 0)
            {
                reasons.Add(
                    $"A Customs Declaration has been submitted however no matching {chedType}(s) have been submitted to Port Health (for {chedType} number(s) {chedNumbers}). Please correct the {chedType} number(s) entered on your customs declaration."
                );
            }
        }
    }

    private static void HandleHmiGmsDecisionReason(
        Commodity item,
        DocumentDecisionResult maxDecisionResult,
        List<string> reasons
    )
    {
        if ((item.Documents == null || !item.Documents.Any()) && maxDecisionResult.DecisionCode == DecisionCode.X00)
        {
            var containsCheckCode = item.Checks != null && item.Checks.Any(x => x.CheckCode == "H220");

            if (containsCheckCode && reasons.Count == 0)
            {
                reasons.Add(
                    "A Customs Declaration with a GMS product has been selected for HMI inspection. In IPAFFS create a CHEDPP and amend your licence to reference it. If a CHEDPP exists, amend your licence to reference it. Failure to do so will delay your Customs release."
                );
            }
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
