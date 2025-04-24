using System.Text.Json;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using SlimMessageBus;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Consumers;

public class ClearanceRequestConsumer(
    ILogger<ClearanceRequestConsumer> logger,
    IDecisionService decisionService,
    ITradeImportsDataApiClient apiClient
) : IConsumer<ResourceEvent<object>>, IConsumerWithContext
{
    public async Task OnHandle(ResourceEvent<object> message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Received notification: {ResourceType}:{ResourceId}",
            message.ResourceType,
            message.ResourceId
        );

        if (
            message.Operation == ResourceEventOperations.Updated
            && !message.ChangeSet.Exists(x => x.Path.Contains("/ClearanceRequest"))
        )
        {
            logger.LogInformation("Skipping Updated Event as ClearanceRequest hasn't changed");
            return;
        }

        var clearanceRequest = await apiClient.GetCustomsDeclaration(message.ResourceId, cancellationToken);

        var notificationResponses = await apiClient.GetImportPreNotificationsByMrn(
            message.ResourceId,
            cancellationToken
        );

        var preNotifications = new List<ImportPreNotification>();

        if (notificationResponses is not null)
        {
            preNotifications = notificationResponses.Select(x => x.ImportPreNotification).ToList();
        }

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

        logger.LogInformation("Decision Derived: {Decision}", JsonSerializer.Serialize(decisionResult));
        await PersistDecision(cancellationToken, clearanceRequest, decisionResult);
    }

    private async Task PersistDecision(
        CancellationToken cancellationToken,
        CustomsDeclarationResponse clearanceRequest,
        DecisionResult decisionResult
    )
    {
        var customsDeclaration = new CustomsDeclaration()
        {
            ClearanceDecision = clearanceRequest.ClearanceDecision,
            Finalisation = clearanceRequest.Finalisation,
            ClearanceRequest = clearanceRequest.ClearanceRequest,
        };

        var newDecision = decisionResult.BuildClearanceDecision(
            clearanceRequest.MovementReferenceNumber,
            customsDeclaration
        );

        if (newDecision.SourceVersion != customsDeclaration.ClearanceDecision?.SourceVersion)
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
}
