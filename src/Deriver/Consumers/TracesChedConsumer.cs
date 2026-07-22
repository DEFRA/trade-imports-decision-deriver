using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Comparers;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Processors;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Consumers;

public class TracesChedConsumer(
    ILogger<TracesChedConsumer> logger,
    ITradeImportsDataApiClient apiClient,
    IDecisionService decisionService
) : ChedConsumer<TracesChedEvent>(apiClient)
{
    public override async Task OnHandle(ResourceEvent<TracesChedEvent> message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Received Ched {ResourceId} with version {Version}",
            message.ResourceId,
            message.Resource?.Ched.GetVersion()
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

        var cheds = await GetTracesCheds(clearanceRequests.Select(x => x.MovementReferenceNumber).Distinct().ToArray());

        var notifications = await GetNotifications(
            clearanceRequests.Select(x => x.MovementReferenceNumber).Distinct().ToArray()
        );

        var decisionResults = decisionService.Process(new DecisionContext(notifications, customsDeclarations, cheds));

        foreach (var result in decisionResults)
        {
            var existingCustomsDeclaration = await ApiClient.GetCustomsDeclaration(result.Mrn, cancellationToken);

            logger.LogInformation(
                "Fetched clearance request {ResourceId} with Etag {Etag} and resource version {Version}",
                result.Mrn,
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

            if (!result.Decision.IsSameAs(customsDeclaration.ClearanceDecision))
            {
                customsDeclaration.ClearanceDecision = result.Decision;

                await ApiClient.PutCustomsDeclaration(
                    result.Mrn,
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

    private async Task<List<CustomsDeclarationWrapper>> GetCustomsDeclarations(
        string chedId,
        CancellationToken cancellationToken
    )
    {
        var customsDeclarations = await ApiClient.GetCustomsDeclarationsByTracesChedId(chedId, cancellationToken);

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

    private async Task<List<DecisionImportPreNotification>> GetNotifications(string[] mrns)
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
                    notifications.Add(notificationResponse);
                }
            }
        );

        return notifications.Select(x => x.ToDecisionImportPreNotification()).ToList();
    }
}
