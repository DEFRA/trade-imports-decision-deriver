using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Comparers;
using Defra.TradeImportsDecisionDeriver.Deriver.Entities;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.Deriver.Utils.CorrelationId;
using SlimMessageBus;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Consumers;

public class ImportPreNotificationConsumer(
    ILogger<ImportPreNotificationConsumer> logger,
    IDecisionService decisionService,
    ITradeImportsDataApiClient apiClient,
    ICorrelationIdGenerator correlationIdGenerator
) : IConsumer<ResourceEvent<ImportPreNotificationEntity>>, IConsumerWithContext
{
    public async Task OnHandle(ResourceEvent<ImportPreNotificationEntity> message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Received notification {ResourceId} with version {Version}",
            message.ResourceId,
            message.Resource?.ImportPreNotification.GetVersion()
        );

        var clearanceRequests = await GetClearanceRequests(message.ResourceId, cancellationToken);
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
        var decisionResult = await decisionService.Process(decisionContext, cancellationToken);

        if (!decisionResult.Decisions.Any())
        {
            logger.LogInformation("No decision derived");

            return;
        }

        logger.LogInformation("Decision derived");

        await PersistDecisions(clearanceRequests, decisionResult, cancellationToken);
    }

    private async Task PersistDecisions(
        List<ClearanceRequestWrapper> clearanceRequests,
        DecisionResult decisionResult,
        CancellationToken cancellationToken
    )
    {
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
    }

    private async Task<List<ClearanceRequestWrapper>> GetClearanceRequests(
        string chedId,
        CancellationToken cancellationToken
    )
    {
        var customsDeclarations = await apiClient.GetCustomsDeclarationsByChedId(chedId, cancellationToken);

        return customsDeclarations
            .CustomsDeclarations.Where(x => x.ClearanceRequest is not null)
            .Where(x => x.Finalisation is null)
            .Select(x => new ClearanceRequestWrapper(x.MovementReferenceNumber, x.ClearanceRequest!))
            .ToList();
    }

    private async Task<List<DecisionImportPreNotification>> GetNotifications(
        ResourceEvent<ImportPreNotificationEntity> message,
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
                                "ImportPreNotification ResourceEvent version does not match API response"
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
