using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Processors;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Consumers;

public class ImportPreNotificationConsumer(
    ILogger<ImportPreNotificationConsumer> logger,
    ITradeImportsDataApiClient apiClient,
    IDecisionService decisionService
) : ChedConsumer<ImportPreNotificationEvent>(apiClient, logger)
{
    private readonly Lock _notificationsLock = new();

    public override async Task OnHandle(
        ResourceEvent<ImportPreNotificationEvent> message,
        CancellationToken cancellationToken
    )
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

        var cheds = await GetTracesCheds(clearanceRequests.Select(x => x.MovementReferenceNumber).Distinct().ToArray());

        var decisionResults = decisionService.Process(new DecisionContext(notifications, customsDeclarations, cheds));

        await ProcessDecisionResult(cancellationToken, decisionResults);
    }

    private async Task<List<CustomsDeclarationWrapper>> GetCustomsDeclarations(
        string chedId,
        CancellationToken cancellationToken
    )
    {
        var customsDeclarations = await ApiClient.GetCustomsDeclarationsByChedId(chedId, cancellationToken);

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
                var apiResponse = await ApiClient.GetImportPreNotificationsByMrn(mrn, cancellationToken);

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
                    lock (_notificationsLock)
                    {
                        notifications.Add(notificationResponse);
                    }

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
}
