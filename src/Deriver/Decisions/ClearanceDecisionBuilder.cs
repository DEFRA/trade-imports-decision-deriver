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
            SourceVersion = decisionResult.BuildDecisionSourceVersion(
                customsDeclaration.ClearanceRequest?.ExternalVersion
            ),
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
                    DecisionReasons = DecisionReasonBuilder.Build(item, maxDecisionResult!).ToArray(),
                    DecisionInternalFurtherDetail = maxDecisionResult.InternalDecisionCode.HasValue
                        ? [maxDecisionResult.InternalDecisionCode.Value.ToString()]
                        : null,
                };
            }
        }
    }
}
