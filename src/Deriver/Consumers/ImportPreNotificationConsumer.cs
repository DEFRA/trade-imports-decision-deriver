using System.Text.Json;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Comparers;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.Processors;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.Deriver.Utils.CorrelationId;
using SlimMessageBus;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Consumers;

public class ImportPreNotificationConsumer(
    ILogger<ImportPreNotificationConsumer> logger,
    IDecisionService decisionService,
    ITradeImportsDataApiClient apiClient,
    ICorrelationIdGenerator correlationIdGenerator,
    IDecisionServiceV2 decisionServiceV2
) : IConsumer<ResourceEvent<ImportPreNotificationEvent>>, IConsumerWithContext
{
    public async Task OnHandle(ResourceEvent<ImportPreNotificationEvent> message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Received notification {ResourceId} with version {Version}",
            message.ResourceId,
            message.Resource?.ImportPreNotification.GetVersion()
        );

        var customsDeclarations = await GetCustomsDeclarations(message.ResourceId, cancellationToken);
        var clearanceRequests = customsDeclarations
            .Select(x => new ClearanceRequestWrapper(x.MovementReferenceNumber, x.CustomsDeclaration.ClearanceRequest!))
            .ToList();
        if (clearanceRequests.Count == 0)
        {
            logger.LogInformation("No decision derived, no customs declaration found");

            return;
        }

        var notifications = await GetNotifications(
            message,
            clearanceRequests.Select(x => x.MovementReferenceNumber).Distinct().ToArray()
        );

        var decisionContext = new DecisionContext(notifications, clearanceRequests);
        var (v1Results, v1Elapsed) = await TimingExtensions.TimeAsync(() =>
            RunV1(decisionContext, clearanceRequests, cancellationToken)
        );

        var (v2Results, v2Elapsed) = TimingExtensions.Time(() =>
            decisionServiceV2.Process(new DecisionContextV2(notifications, customsDeclarations))
        );

        logger.LogInformation("V1 Took {V1Elapsed} - V2 took {V2Elapsed}", v1Elapsed, v2Elapsed);

        foreach (var v1Result in v1Results)
        {
            var v2Result = v2Results.FirstOrDefault(x => x.Mrn == v1Result.Mrn);

            if (!v1Result.Decision.IsSameAs(v2Result.Decision))
            {
                logger.LogInformation(
                    "MRN {Mrn} - V1 {V1} - V2 {V2}",
                    v1Result.Mrn,
                    JsonSerializer.Serialize(v1Result),
                    JsonSerializer.Serialize(v2Result)
                );
            }
        }
    }

    private async Task<List<(string Mrn, ClearanceDecision Decision)>> RunV1(
        DecisionContext decisionContext,
        List<ClearanceRequestWrapper> clearanceRequests,
        CancellationToken cancellationToken
    )
    {
        var decisionResult = await decisionService.Process(decisionContext, cancellationToken);

        if (!decisionResult.Decisions.Any())
        {
            logger.LogInformation("No decision derived");

            return [];
        }

        logger.LogInformation("Decision derived");

        var oldResults = await PersistDecisions(clearanceRequests, decisionResult, cancellationToken);
        return oldResults;
    }

    private async Task<List<(string Mrn, ClearanceDecision Decision)>> PersistDecisions(
        List<ClearanceRequestWrapper> clearanceRequests,
        DecisionResult decisionResult,
        CancellationToken cancellationToken
    )
    {
        var output = new List<(string Mrn, ClearanceDecision Decision)>(clearanceRequests.Count);
        foreach (var mrn in clearanceRequests.Select(x => x.MovementReferenceNumber))
        {
            var existingCustomsDeclaration = await apiClient.GetCustomsDeclaration(mrn, cancellationToken);

            logger.LogInformation(
                "Fetched clearance request {ResourceId} with Etag {Etag} and resource version {Version}",
                mrn,
                existingCustomsDeclaration?.ETag,
                existingCustomsDeclaration?.ClearanceRequest.GetVersion()
            );

            var customsDeclaration = new CustomsDeclaration
            {
                ClearanceDecision = existingCustomsDeclaration?.ClearanceDecision,
                Finalisation = existingCustomsDeclaration?.Finalisation,
                ClearanceRequest = existingCustomsDeclaration?.ClearanceRequest,
                ExternalErrors = existingCustomsDeclaration?.ExternalErrors,
            };

            var newDecision = decisionResult.BuildClearanceDecision(mrn, customsDeclaration, correlationIdGenerator);
            output.Add((mrn, newDecision));

            if (!newDecision.IsSameAs(customsDeclaration.ClearanceDecision))
            {
                customsDeclaration.ClearanceDecision = newDecision;

                await apiClient.PutCustomsDeclaration(
                    mrn,
                    customsDeclaration,
                    existingCustomsDeclaration?.ETag,
                    cancellationToken
                );
            }
            else
            {
                logger.LogInformation("Decision already exists, not persisting");
            }
        }

        return output;
    }

    private async Task<List<CustomsDeclarationWrapper>> GetCustomsDeclarations(
        string chedId,
        CancellationToken cancellationToken
    )
    {
        var customsDeclarations = await apiClient.GetCustomsDeclarationsByChedId(chedId, cancellationToken);

        return customsDeclarations
            .CustomsDeclarations.Where(x => x.ClearanceRequest is not null)
            .Where(x => x.Finalisation is null)
            .Select(x => new CustomsDeclarationWrapper(
                x.MovementReferenceNumber,
                new CustomsDeclaration()
                {
                    ClearanceDecision = x.ClearanceDecision,
                    ClearanceRequest = x.ClearanceRequest,
                }
            ))
            .ToList();
    }

    private async Task<List<DecisionImportPreNotification>> GetNotifications(
        ResourceEvent<ImportPreNotificationEvent> message,
        string[] mrns
    )
    {
        var notifications = new List<ImportPreNotification>();

        await Parallel.ForEachAsync(
            mrns,
            async (mrn, cancellationToken) =>
            {
                var apiResponse = await apiClient.GetImportPreNotificationsByMrn(mrn, cancellationToken);

                foreach (
                    var notificationResponse in apiResponse
                        .ImportPreNotifications.Where(notificationResponse =>
                            !notifications.Exists(x =>
                                x.ReferenceNumber == notificationResponse.ImportPreNotification.ReferenceNumber
                            )
                        )
                        .Select(x => x.ImportPreNotification)
                )
                {
                    notifications.Add(notificationResponse);

                    if (message.ResourceId == notificationResponse.ReferenceNumber)
                    {
                        if (message.Resource?.ImportPreNotification.GetVersion() != notificationResponse.GetVersion())
                        {
                            logger.LogInformation(
                                message.Resource?.ImportPreNotification.UpdatedSource.TrimMicroseconds()
                                > notificationResponse.UpdatedSource.TrimMicroseconds()
                                    ? "ImportPreNotification ResourceEvent version does not match API response : ResourceEvent is newer"
                                    : "ImportPreNotification ResourceEvent version does not match API response : API response is newer"
                            );
                        }
                    }
                    else
                    {
                        logger.LogInformation("ImportPreNotification ResourceEvent version matches API response");
                    }
                }
            }
        );

        return notifications.Select(x => x.ToDecisionImportPreNotification()).ToList();
    }

    public IConsumerContext Context { get; set; } = null!;
}
