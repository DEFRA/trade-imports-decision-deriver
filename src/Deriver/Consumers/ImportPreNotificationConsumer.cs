using System.Text.Json;
using System.Text.Json.Serialization;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using SlimMessageBus;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Consumers;

public class ImportPreNotificationConsumer(
    ILogger<ImportPreNotificationConsumer> logger,
    IDecisionService decisionService,
    ITradeImportsDataApiClient apiClient
) : IConsumer<ResourceEvent<object>>, IConsumerWithContext
{
    private static JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public async Task OnHandle(ResourceEvent<object> message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Received notification: {ResourceType}:{ResourceId}",
            message.ResourceType,
            message.ResourceId
        );
        var clearanceRequests = await GetClearanceRequests(message.ResourceId, cancellationToken);

        if (!clearanceRequests.Any())
        {
            logger.LogInformation(
                "No Decision Derived, because no Customs Declaration found for {ChedId}",
                message.ResourceId
            );
            return;
        }

        var notifications = await GetNotifications(
            clearanceRequests.Select(x => x.MovementReferenceNumber).Distinct().ToArray()
        );

        var decisionContext = new DecisionContext(notifications, clearanceRequests);
        var decisionResult = await decisionService.Process(decisionContext, cancellationToken);

        if (!decisionResult.Decisions.Any())
        {
            logger.LogInformation(
                "No Decision Derived: {ResourceType}:{ResourceId}",
                message.ResourceType,
                message.ResourceId
            );
            return;
        }

        logger.LogInformation(
            "Decision Derived: {Decision} with {DecisionContext}",
            JsonSerializer.Serialize(decisionResult, _jsonSerializerOptions),
            JsonSerializer.Serialize(decisionContext, _jsonSerializerOptions)
        );
        await PersistDecisions(cancellationToken, clearanceRequests, decisionResult);
    }

    private async Task PersistDecisions(
        CancellationToken cancellationToken,
        List<ClearanceRequestWrapper> clearanceRequests,
        DecisionResult decisionResult
    )
    {
        foreach (var mrn in clearanceRequests.Select(x => x.MovementReferenceNumber))
        {
            var clearanceRequest = await apiClient.GetCustomsDeclaration(mrn, cancellationToken);

            var customsDeclaration = new CustomsDeclaration()
            {
                ClearanceDecision = clearanceRequest?.ClearanceDecision,
                Finalisation = clearanceRequest?.Finalisation,
                ClearanceRequest = clearanceRequest?.ClearanceRequest,
            };

            var newDecision = decisionResult.BuildClearanceDecision(mrn, customsDeclaration);

            if (newDecision.SourceVersion != customsDeclaration.ClearanceDecision?.SourceVersion)
            {
                customsDeclaration.ClearanceDecision = newDecision;
                await apiClient.PutCustomsDeclaration(
                    mrn,
                    customsDeclaration,
                    clearanceRequest?.ETag,
                    cancellationToken
                );
            }
        }
    }

    private async Task<List<ClearanceRequestWrapper>> GetClearanceRequests(
        string chedId,
        CancellationToken cancellationToken
    )
    {
        var customsDeclarations = await apiClient.GetCustomsDeclarationsByChedId(chedId, cancellationToken);

        if (customsDeclarations == null)
        {
            return [];
        }

        return customsDeclarations
            .Where(x => x.ClearanceRequest is not null)
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
                if (apiResponse != null)
                {
                    foreach (
                        var notificationResponse in apiResponse.Where(notificationResponse =>
                            !notifications.Exists(x =>
                                x.ReferenceNumber == notificationResponse.ImportPreNotification.ReferenceNumber
                            )
                        )
                    )
                    {
                        notifications.Add(notificationResponse.ImportPreNotification);
                    }
                }
            }
        );
        return notifications.Select(x => x.ToDecisionImportPreNotification()).ToList();
    }

    public IConsumerContext Context { get; set; } = null!;
}
