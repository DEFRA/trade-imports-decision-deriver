using System.Text.Json;
using System.Text.Json.Serialization;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Comparers;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using SlimMessageBus;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Consumers;

public class ClearanceRequestConsumer(
    ILogger<ClearanceRequestConsumer> logger,
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
            "Received notification: {SubResourceType}:{ResourceId}",
            message.SubResourceType,
            message.ResourceId
        );

        var clearanceRequest = await apiClient.GetCustomsDeclaration(message.ResourceId, cancellationToken);

        if (WasFinalisedBeforeClearanceRequest(clearanceRequest))
        {
            logger.LogInformation(
                "Skipping Event : {SubResourceType}:{ResourceId} as has been finalised",
                message.SubResourceType,
                message.ResourceId
            );
            return;
        }

        var notificationResponse = await apiClient.GetImportPreNotificationsByMrn(
            message.ResourceId,
            cancellationToken
        );

        var preNotifications = notificationResponse
            .ImportPreNotifications.Select(x => x.ImportPreNotification)
            .ToList();

        var decisionContext = new DecisionContext(
            preNotifications.Select(x => x.ToDecisionImportPreNotification()).ToList(),
            [new ClearanceRequestWrapper(message.ResourceId, clearanceRequest!.ClearanceRequest!)]
        );
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
            "Decision Derived: {Decision}",
            JsonSerializer.Serialize(decisionResult, _jsonSerializerOptions)
        );
        await PersistDecision(clearanceRequest, decisionResult, cancellationToken);
    }

    private async Task PersistDecision(
        CustomsDeclarationResponse clearanceRequest,
        DecisionResult decisionResult,
        CancellationToken cancellationToken
    )
    {
        var customsDeclaration = new CustomsDeclaration()
        {
            ClearanceDecision = clearanceRequest.ClearanceDecision,
            Finalisation = clearanceRequest.Finalisation,
            ClearanceRequest = clearanceRequest.ClearanceRequest,
            InboundError = clearanceRequest.InboundError,
        };

        var newDecision = decisionResult.BuildClearanceDecision(
            clearanceRequest.MovementReferenceNumber,
            customsDeclaration
        );

        if (!ClearanceDecisionComparer.Default.Equals(newDecision, customsDeclaration.ClearanceDecision))
        {
            customsDeclaration.ClearanceDecision = newDecision;

            await apiClient.PutCustomsDeclaration(
                clearanceRequest.MovementReferenceNumber,
                customsDeclaration,
                clearanceRequest.ETag,
                cancellationToken
            );
        }
    }

    public IConsumerContext Context { get; set; } = null!;

    private static bool WasFinalisedBeforeClearanceRequest(CustomsDeclarationResponse? customsDeclaration)
    {
        return customsDeclaration is { ClearanceRequest: not null, Finalisation: not null }
            && customsDeclaration.ClearanceRequest.MessageSentAt > customsDeclaration.Finalisation.MessageSentAt;
    }
}
