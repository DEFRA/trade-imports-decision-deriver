using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

public class DecisionService(
    ILogger<DecisionService> logger,
    IMatchingService matchingService,
    IEnumerable<IDecisionFinder> decisionFinders
) : IDecisionService
{
    public async Task<DecisionResult> Process(DecisionContext decisionContext, CancellationToken cancellationToken)
    {
        decisionContext.LogVersions(logger);
        var matchResult = await matchingService.Process(
            new MatchingContext(decisionContext.Notifications, decisionContext.ClearanceRequests),
            cancellationToken
        );
        var decisionResult = await DeriveDecision(decisionContext, matchResult);
        return decisionResult;
    }

    private Task<DecisionResult> DeriveDecision(DecisionContext decisionContext, MatchingResult matchingResult)
    {
        var decisionsResult = new DecisionResult();

        foreach (var wrapper in decisionContext.ClearanceRequests)
        {
            if (wrapper.ClearanceRequest.Commodities == null)
                continue;

            foreach (
                var item in wrapper.ClearanceRequest.Commodities.Where(x =>
                    HasChecks(decisionContext, wrapper.MovementReferenceNumber, x.ItemNumber!.Value)
                )
            )
            {
                var checkCodes = wrapper
                    .ClearanceRequest.Commodities.First(x => x.ItemNumber == item.ItemNumber!.Value)
                    .Checks!.Select(x => x.CheckCode)
                    .Where(x => x != null)
                    .Cast<string>()
                    .Select(x => new CheckCode { Value = x })
                    .ToArray();

                HandleNoMatches(matchingResult, item, wrapper, checkCodes, decisionsResult);
                HandleMatches(decisionContext, matchingResult, item, wrapper, checkCodes, decisionsResult);
                HandleOrphanChecks(decisionsResult, item, checkCodes, wrapper);
            }
        }

        return Task.FromResult(decisionsResult);
    }

    private void HandleMatches(
        DecisionContext decisionContext,
        MatchingResult matchingResult,
        Commodity item,
        ClearanceRequestWrapper wrapper,
        CheckCode[] checkCodes,
        DecisionResult decisionsResult
    )
    {
        var itemNumber = item.ItemNumber!.Value;
        var matches = matchingResult
            .Matches.Where(x => x.ItemNumber == itemNumber && x.Mrn == wrapper.MovementReferenceNumber)
            .ToList();

        foreach (var match in matches)
        {
            var notification = decisionContext.Notifications.First(x => x.Id == match.ImportPreNotificationId);
            var decisionCodes = GetDecisions(notification, checkCodes, match.DocumentCode);

            foreach (var decisionCode in decisionCodes)
            {
                decisionsResult.AddDecision(
                    match.Mrn,
                    match.ItemNumber,
                    match.DocumentReference,
                    match.DocumentCode,
                    decisionCode.CheckCode?.Value,
                    decisionCode.DecisionCode,
                    notification,
                    decisionCode.InternalDecisionCode
                );
            }
        }
    }

    private static void HandleOrphanChecks(
        DecisionResult decisionsResult,
        Commodity item,
        CheckCode[] checkCodes,
        ClearanceRequestWrapper wrapper
    )
    {
        var decisionChecks = decisionsResult
            .Decisions.Where(x => x.ItemNumber == item.ItemNumber)
            .Select(x => x.CheckCode)
            .Distinct()
            .ToArray();

        var orphanCheckCodes = checkCodes.Select(x => x.Value).Except(decisionChecks).ToArray();
        foreach (var checkCode in orphanCheckCodes)
        {
            var internalFurtherDetail =
                checkCode == "H220" && checkCodes.Any(x => x.Value == "H219")
                    ? DecisionInternalFurtherDetail.E82
                    : DecisionInternalFurtherDetail.E83;

            decisionsResult.AddDecision(
                wrapper.MovementReferenceNumber!,
                item.ItemNumber!.Value,
                string.Empty,
                null,
                checkCode,
                DecisionCode.X00,
                internalDecisionCode: internalFurtherDetail
            );
        }
    }

    private static void HandleNoMatches(
        MatchingResult matchingResult,
        Commodity item,
        ClearanceRequestWrapper wrapper,
        CheckCode[] checkCodes,
        DecisionResult decisionsResult
    )
    {
        var itemNumber = item.ItemNumber!.Value;
        var noMatches = matchingResult
            .NoMatches.Where(x => x.ItemNumber == itemNumber && x.Mrn == wrapper.MovementReferenceNumber)
            .ToList();

        foreach (var noMatch in noMatches)
        {
            HandleNoMatch(checkCodes, decisionsResult, noMatch);
        }
    }

    private static void HandleNoMatch(CheckCode[] checkCodes, DecisionResult decisionsResult, DocumentNoMatch noMatch)
    {
        foreach (var checkCode in checkCodes.Select(checkCode => checkCode))
        {
            if (!checkCode.IsValidDocumentCode(noMatch.DocumentCode))
            {
                continue;
            }
            if (checkCode.Value is "H218" or "H219" or "H220")
            {
                switch (checkCode.Value)
                {
                    case "H219" when noMatch.DocumentCode is "N851" or "9115" or "C085":
                    case "H218"
                    or "H220" when noMatch.DocumentCode is "N002" or "C085":
                        decisionsResult.AddDecision(
                            noMatch.Mrn,
                            noMatch.ItemNumber,
                            noMatch.DocumentReference,
                            noMatch.DocumentCode,
                            checkCode.Value,
                            DecisionCode.X00,
                            internalDecisionCode: DecisionInternalFurtherDetail.E70
                        );
                        break;
                }
            }
            else
            {
                decisionsResult.AddDecision(
                    noMatch.Mrn,
                    noMatch.ItemNumber,
                    noMatch.DocumentReference,
                    noMatch.DocumentCode,
                    checkCode.Value,
                    DecisionCode.X00,
                    internalDecisionCode: DecisionInternalFurtherDetail.E70
                );
            }
        }
    }

    private DecisionFinderResult[] GetDecisions(
        DecisionImportPreNotification notification,
        CheckCode[] checkCodes,
        string? documentCode
    )
    {
        var results = new List<DecisionFinderResult>();

        var finders = GetDecisionsFindersForCheckCodes(notification, checkCodes, documentCode).ToList();

        if (finders.Count == 0)
        {
            foreach (var checkCode in checkCodes)
            {
                logger.LogWarning(
                    "No decision finder count for notification {Id} and check code {CheckCode} and document code {DocumentCode}",
                    notification.Id,
                    checkCode,
                    documentCode
                );
            }
        }
        else
        {
            foreach (var checkCode in checkCodes)
            {
                results.AddRange(GetDecisionsForCheckCode(notification, checkCode, documentCode, finders));
            }
        }

        var item = 1;

        foreach (var result in results)
            logger.LogInformation(
                "Decision finder result {ItemNum} of {NumItems} for notification {Id} decision {Decision}, InternalCode {InternalCode} DecisionEnum {DecisionEnum}, NotAcceptableAction {NotAcceptableAction}",
                item++,
                results.Count,
                notification.Id,
                result.DecisionCode,
                result.InternalDecisionCode,
                notification.ConsignmentDecision,
                notification.NotAcceptableAction
            );

        return results.ToArray();
    }

    private static IEnumerable<DecisionFinderResult> GetDecisionsForCheckCode(
        DecisionImportPreNotification notification,
        CheckCode checkCode,
        string? documentCode,
        IEnumerable<IDecisionFinder> decisionFinders
    )
    {
        var finders = decisionFinders
            .DistinctBy(x => x.GetType())
            .Where(x => x.CanFindDecision(notification, checkCode, documentCode))
            .ToArray();

        if (finders.Length > 1)
        {
            finders = finders.Where(f => f.ChedType == checkCode.GetImportNotificationType()).ToArray();
        }

        foreach (var finder in finders)
        {
            yield return finder.FindDecision(notification, checkCode);
        }
    }

    private IEnumerable<IDecisionFinder> GetDecisionsFindersForCheckCodes(
        DecisionImportPreNotification notification,
        CheckCode[] checkCodes,
        string? documentCode
    )
    {
        return checkCodes.SelectMany(checkCode =>
            decisionFinders.Where(x => x.CanFindDecision(notification, checkCode, documentCode))
        );
    }

    private static bool HasChecks(DecisionContext decisionContext, string mrn, int itemNumber)
    {
        var clearanceRequestCommodities = decisionContext
            .ClearanceRequests.First(x => x.MovementReferenceNumber == mrn)
            .ClearanceRequest.Commodities;

        if (clearanceRequestCommodities != null)
        {
            var checks = clearanceRequestCommodities.First(x => x.ItemNumber == itemNumber).Checks;
            return checks != null && checks.Length != 0;
        }

        return false;
    }
}
