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
            if (wrapper.ClearanceRequest?.Commodities != null)
            {
                foreach (
                    var item in wrapper.ClearanceRequest.Commodities.Where(x =>
                        HasChecks(decisionContext, wrapper.MovementReferenceNumber, x.ItemNumber!.Value)
                    )
                )
                {
                    var checkCodes = wrapper
                        .ClearanceRequest.Commodities.First(x => x.ItemNumber == item.ItemNumber!.Value)
                        .Checks?.Select(x => x.CheckCode)
                        .Where(x => x != null)
                        .Cast<string>()
                        .Select(x => new CheckCode() { Value = x })
                        .ToArray();
                    HandleNoMatches(matchingResult, item, wrapper, checkCodes, decisionsResult);

                    HandleMatches(decisionContext, matchingResult, item, wrapper, checkCodes, decisionsResult);

                    HandleItemsWithInvalidReference(wrapper.MovementReferenceNumber!, item, decisionsResult);
                }
            }
        }

        return Task.FromResult(decisionsResult);
    }

    private void HandleMatches(
        DecisionContext decisionContext,
        MatchingResult matchingResult,
        Commodity item,
        ClearanceRequestWrapper wrapper,
        CheckCode[]? checkCodes,
        DecisionResult decisionsResult
    )
    {
        int itemNumber = item.ItemNumber!.Value;
        var matches = matchingResult
            .Matches.Where(x => x.ItemNumber == itemNumber && x.Mrn == wrapper.MovementReferenceNumber)
            .ToList();

        foreach (var match in matches)
        {
            var notification = decisionContext.Notifications.First(x => x.Id == match.ImportPreNotificationId);

            var decisionCodes = GetDecisions(notification, checkCodes);
            foreach (var decisionCode in decisionCodes)
            {
                decisionsResult.AddDecision(
                    match.Mrn,
                    match.ItemNumber,
                    match.DocumentReference,
                    decisionCode.CheckCode?.Value,
                    decisionCode.DecisionCode,
                    notification,
                    decisionCode.DecisionReason,
                    decisionCode.InternalDecisionCode
                );
            }
        }
    }

    private static void HandleNoMatches(
        MatchingResult matchingResult,
        Commodity item,
        ClearanceRequestWrapper wrapper,
        CheckCode[]? checkCodes,
        DecisionResult decisionsResult
    )
    {
        int itemNumber = item.ItemNumber!.Value;
        var noMatches = matchingResult
            .NoMatches.Where(x => x.ItemNumber == itemNumber && x.Mrn == wrapper.MovementReferenceNumber)
            .ToList();

        foreach (var noMatch in noMatches)
        {
            HandleNoMatch(checkCodes, decisionsResult, noMatch);
        }
    }

    private static void HandleNoMatch(CheckCode[]? checkCodes, DecisionResult decisionsResult, DocumentNoMatch noMatch)
    {
        if (checkCodes != null)
        {
            foreach (var checkCode in checkCodes.Select(checkCode => checkCode.Value))
            {
                string? reason = null;

                if (checkCode is "H220")
                {
                    reason =
                        "A Customs Declaration with a GMS product has been selected for HMI inspection. In IPAFFS create a CHEDPP and amend your licence to reference it. If a CHEDPP exists, amend your licence to reference it. Failure to do so will delay your Customs release";
                }

                decisionsResult.AddDecision(
                    noMatch.Mrn,
                    noMatch.ItemNumber,
                    noMatch.DocumentReference,
                    checkCode,
                    DecisionCode.X00,
                    decisionReason: reason
                );
            }
        }
        else
        {
            decisionsResult.AddDecision(
                noMatch.Mrn,
                noMatch.ItemNumber,
                noMatch.DocumentReference,
                null,
                DecisionCode.X00
            );
        }
    }

    private static void HandleItemsWithInvalidReference(string mrn, Commodity item, DecisionResult decisionsResult)
    {
        int itemNumber = item.ItemNumber!.Value;
        var decisions = decisionsResult.Decisions.Where(x => x.ItemNumber == itemNumber && x.Mrn == mrn).ToList();

        if (!decisions.Any())
        {
            foreach (var document in item.Documents!)
            {
                decisionsResult.AddDecision(
                    mrn,
                    itemNumber,
                    document.DocumentReference!.Value,
                    null,
                    DecisionCode.X00,
                    internalDecisionCode: DecisionInternalFurtherDetail.E89
                );
            }
        }
    }

    private DecisionFinderResult[] GetDecisions(DecisionImportPreNotification notification, CheckCode[]? checkCodes)
    {
        var results = new List<DecisionFinderResult>();
        if (checkCodes == null)
        {
            results.AddRange(GetDecisionsForCheckCode(notification, null, decisionFinders));
        }
        else
        {
            var finders = GetDecisionsFindersForCheckCodes(notification, checkCodes).ToList();

            if (!finders.Any())
            {
                foreach (var checkCode in checkCodes)
                {
                    logger.LogWarning(
                        "No Decision Finder count for ImportNotification {Id} and Check code {CheckCode}",
                        notification.Id,
                        checkCode
                    );
                    results.Add(
                        new DecisionFinderResult(
                            DecisionCode.X00,
                            checkCode,
                            InternalDecisionCode: DecisionInternalFurtherDetail.E90
                        )
                    );
                }
            }
            else
            {
                foreach (var checkCode in checkCodes)
                {
                    results.AddRange(GetDecisionsForCheckCode(notification, checkCode, finders));
                }
            }
        }

        var item = 1;
        foreach (var result in results)
            logger.LogInformation(
                "Decision finder result {ItemNum} of {NumItems} for Notification {Id} Decision {Decision} - ConsignmentAcceptable {ConsignmentAcceptable}: DecisionEnum {DecisionEnum}: NotAcceptableAction {NotAcceptableAction}",
                item++,
                results.Count,
                notification.Id,
                result.DecisionCode.ToString(),
                notification.ConsignmentAcceptable,
                notification.ConsignmentDecision.ToString(),
                notification.NotAcceptableAction?.ToString()
            );

        return results.ToArray();
    }

    private static IEnumerable<DecisionFinderResult> GetDecisionsForCheckCode(
        DecisionImportPreNotification notification,
        CheckCode? checkCode,
        IEnumerable<IDecisionFinder> decisionFinders
    )
    {
        var finders = decisionFinders.Where(x => x.CanFindDecision(notification, checkCode)).ToArray();

        foreach (var finder in finders)
        {
            yield return finder.FindDecision(notification, checkCode);
        }
    }

    private IEnumerable<IDecisionFinder> GetDecisionsFindersForCheckCodes(
        DecisionImportPreNotification notification,
        CheckCode[] checkCodes
    )
    {
        return checkCodes.SelectMany(checkCode =>
            decisionFinders.Where(x => x.CanFindDecision(notification, checkCode))
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
            return checks != null && checks.Any();
        }

        return false;
    }
}
