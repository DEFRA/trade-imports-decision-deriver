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
    public async Task OnHandle(ResourceEvent<object> message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Received clearance request {ResourceId} of sub type {SubResourceType}",
            message.ResourceId,
            message.SubResourceType
        );

        var clearanceRequest = await apiClient.GetCustomsDeclaration(message.ResourceId, cancellationToken);

        if (WasFinalisedBeforeClearanceRequest(clearanceRequest))
        {
            logger.LogInformation("Skipping, already finalised");

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
            logger.LogInformation("No decision derived");

            return;
        }

        logger.LogInformation("Decision derived");

        await PersistDecision(clearanceRequest, decisionResult, cancellationToken);
    }

    private async Task PersistDecision(
        CustomsDeclarationResponse existingCustomsDeclaration,
        DecisionResult decisionResult,
        CancellationToken cancellationToken
    )
    {
        var customsDeclaration = new CustomsDeclaration
        {
            ClearanceDecision = existingCustomsDeclaration.ClearanceDecision,
            Finalisation = existingCustomsDeclaration.Finalisation,
            ClearanceRequest = existingCustomsDeclaration.ClearanceRequest,
            ExternalErrors = existingCustomsDeclaration.ExternalErrors,
        };

        var newDecision = decisionResult.BuildClearanceDecision(
            existingCustomsDeclaration.MovementReferenceNumber,
            customsDeclaration
        );

        if (!ClearanceDecisionComparer.Default.Equals(newDecision, customsDeclaration.ClearanceDecision))
        {
            customsDeclaration.ClearanceDecision = newDecision;

            await apiClient.PutCustomsDeclaration(
                existingCustomsDeclaration.MovementReferenceNumber,
                customsDeclaration,
                existingCustomsDeclaration.ETag,
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
