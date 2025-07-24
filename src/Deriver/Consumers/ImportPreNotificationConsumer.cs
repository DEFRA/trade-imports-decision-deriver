using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Comparers;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.Deriver.Utils.CorrelationId;
using SlimMessageBus;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Consumers;

public class ImportPreNotificationConsumer(
    ILogger<ImportPreNotificationConsumer> logger,
    IDecisionService decisionService,
    ITradeImportsDataApiClient apiClient,
    ICorrelationIdGenerator correlationIdGenerator
) : IConsumer<ResourceEvent<object>>, IConsumerWithContext
{
    public async Task OnHandle(ResourceEvent<object> message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received notification {ResourceId}", message.ResourceId);

        var clearanceRequests = await GetClearanceRequests(message.ResourceId, cancellationToken);
        if (clearanceRequests.Count == 0)
        {
            logger.LogInformation("No decision derived, no customs declaration found");

            return;
        }

        var notifications = await GetNotifications(
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

            var customsDeclaration = new CustomsDeclaration
            {
                ClearanceDecision = existingCustomsDeclaration?.ClearanceDecision,
                Finalisation = existingCustomsDeclaration?.Finalisation,
                ClearanceRequest = existingCustomsDeclaration?.ClearanceRequest,
                ExternalErrors = existingCustomsDeclaration?.ExternalErrors,
            };

            var newDecision = decisionResult.BuildClearanceDecision(mrn, customsDeclaration, correlationIdGenerator);

            if (!DecisionExistsComparer.Default.Equals(newDecision, customsDeclaration.ClearanceDecision))
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

    private async Task<List<DecisionImportPreNotification>> GetNotifications(string[] mrns)
    {
        var notifications = new List<ImportPreNotification>();

        await Parallel.ForEachAsync(
            mrns,
            async (mrn, cancellationToken) =>
            {
                var apiResponse = await apiClient.GetImportPreNotificationsByMrn(mrn, cancellationToken);

                foreach (
                    var notificationResponse in apiResponse.ImportPreNotifications.Where(notificationResponse =>
                        !notifications.Exists(x =>
                            x.ReferenceNumber == notificationResponse.ImportPreNotification.ReferenceNumber
                        )
                    )
                )
                {
                    notifications.Add(notificationResponse.ImportPreNotification);
                }
            }
        );

        return notifications.Select(x => x.ToDecisionImportPreNotification()).ToList();
    }

    public IConsumerContext Context { get; set; } = null!;
}
