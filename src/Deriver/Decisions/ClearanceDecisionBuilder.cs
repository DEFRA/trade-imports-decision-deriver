using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Utils.CorrelationId;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

public static class ClearanceDecisionBuilder
{
    public static ClearanceDecision BuildClearanceDecision(
        this DecisionResult decisionResult,
        string mrn,
        CustomsDeclaration customsDeclaration,
        ICorrelationIdGenerator correlationIdGenerator
    )
    {
        var decisions = decisionResult.Decisions.Where(x => x.Mrn == mrn).ToList();

        return new ClearanceDecision
        {
            DecisionNumber = customsDeclaration.ClearanceDecision is { DecisionNumber: not null }
                ? customsDeclaration.ClearanceDecision.DecisionNumber + 1
                : 1,
            Created = DateTime.UtcNow,
            CorrelationId = correlationIdGenerator.Generate(),
            ExternalVersionNumber = customsDeclaration.ClearanceRequest?.ExternalVersion,
            Items = BuildItems(customsDeclaration.ClearanceRequest!, decisions).ToArray(),
            Results = decisions
                .Select(x => new ClearanceDecisionResult
                {
                    ItemNumber = x.ItemNumber,
                    ImportPreNotification = x.PreNotification?.Id,
                    DocumentReference = x.DocumentReference,
                    DocumentCode = x.DocumentCode,
                    CheckCode = x.CheckCode,
                    DecisionCode = x.DecisionCode.ToString(),
                    DecisionReason = x.DecisionReason,
                    InternalDecisionCode = x.InternalDecisionCode?.ToString(),
                })
                .ToArray(),
        };
    }

    private static IEnumerable<ClearanceDecisionItem> BuildItems(
        ClearanceRequest clearanceRequest,
        List<DocumentDecisionResult> movementDecisions
    )
    {
        if (clearanceRequest is not null)
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
                        Checks = BuildChecks(clearanceRequest, commodity, itemDecisions).ToArray(),
                    };
                }
            }
        }
    }

    private static IEnumerable<ClearanceDecisionCheck> BuildChecks(
        ClearanceRequest clearanceRequest,
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

            var documentResultsForItem = itemDecisions.Where(x => x.ItemNumber == item.ItemNumber).ToArray();
            if (maxDecisionResult is not null)
            {
                yield return new ClearanceDecisionCheck
                {
                    CheckCode = checkCode,
                    DecisionCode = maxDecisionResult.DecisionCode.ToString(),
                    DecisionReasons = DecisionReasonBuilder
                        .Build(clearanceRequest, item, maxDecisionResult!, documentResultsForItem)
                        .ToArray(),
                    DecisionInternalFurtherDetail = maxDecisionResult.InternalDecisionCode.HasValue
                        ? [maxDecisionResult.InternalDecisionCode.Value.ToString()]
                        : null,
                };
            }
        }
    }
}
