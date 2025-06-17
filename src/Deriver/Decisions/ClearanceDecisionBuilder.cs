using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

public static class ClearanceDecisionBuilder
{
    public static ClearanceDecision BuildClearanceDecision(
        this DecisionResult decisionResult,
        string mrn,
        CustomsDeclaration customsDeclaration
    )
    {
        var decisions = decisionResult.Decisions.Where(x => x.Mrn == mrn).ToList();

        return new ClearanceDecision
        {
            DecisionNumber = customsDeclaration.ClearanceDecision is { DecisionNumber: not null }
                ? customsDeclaration.ClearanceDecision.DecisionNumber + 1
                : 1,
            SourceVersion = decisionResult.BuildDecisionSourceVersion(
                customsDeclaration.ClearanceRequest?.ExternalVersion
            ),
            Created = DateTime.UtcNow,
            // logic to generate the correlation Id will need to be applied here when HMRC have confirmed the proposed approach
            CorrelationId = CorrelationId.GenerateNewId().ToString(),
            ExternalVersionNumber = customsDeclaration.ClearanceRequest?.ExternalVersion,
            Items = BuildItems(customsDeclaration.ClearanceRequest!, decisions).ToArray(),
        };
    }

    private static IEnumerable<ClearanceDecisionItem> BuildItems(
        ClearanceRequest clearanceRequest,
        List<DocumentDecisionResult> movementDecisions
    )
    {
        var decisionsByItem = movementDecisions.GroupBy(x => x.ItemNumber);
        foreach (var itemDecisions in decisionsByItem)
        {
            if (clearanceRequest.Commodities != null)
            {
                var commodity = clearanceRequest.Commodities.First(x => x.ItemNumber == itemDecisions.Key);
                yield return new ClearanceDecisionItem
                {
                    ItemNumber = itemDecisions.Key,
                    Checks = BuildChecks(commodity, itemDecisions).ToArray(),
                };
            }
        }
    }

    private static IEnumerable<ClearanceDecisionCheck> BuildChecks(
        Commodity item,
        IGrouping<int, DocumentDecisionResult> itemDecisions
    )
    {
        if (item.Checks == null)
            yield break;

        foreach (var checkCode in item.Checks.Select(x => x.CheckCode!))
        {
            var maxDecisionResult = itemDecisions
                .Where(x => x.CheckCode == null || x.CheckCode == checkCode)
                .OrderByDescending(x => x.DecisionCode)
                .FirstOrDefault();
            if (maxDecisionResult is not null)
            {
                yield return new ClearanceDecisionCheck
                {
                    CheckCode = checkCode,
                    DecisionCode = maxDecisionResult.DecisionCode.ToString(),
                    DecisionReasons = BuildDecisionReasons(item, maxDecisionResult!),
                    DecisionInternalFurtherDetail = maxDecisionResult.InternalDecisionCode.HasValue
                        ? [maxDecisionResult.InternalDecisionCode.Value.ToString()]
                        : null,
                };
            }
        }
    }

    private static string[] BuildDecisionReasons(Commodity item, DocumentDecisionResult maxDecisionResult)
    {
        var reasons = new List<string>();

        if (maxDecisionResult.DecisionReason != null)
        {
            reasons.Add(maxDecisionResult.DecisionReason);
        }

        if (maxDecisionResult.DecisionCode == DecisionCode.X00)
        {
            var chedType = MapToChedType(item.Documents?[0]);
            var chedNumbers = string.Join(", ", item.Documents!.Select(x => x.DocumentReference?.Value));

            if (reasons.Count == 0)
            {
                reasons.Add(
                    $"A Customs Declaration has been submitted however no matching {chedType}(s) have been submitted to Port Health (for {chedType} number(s) {chedNumbers}). Please correct the {chedType} number(s) entered on your customs declaration."
                );
            }
        }

        return reasons.ToArray();
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
