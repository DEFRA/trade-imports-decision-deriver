using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Comparers;
using Defra.TradeImportsDecisionDeriver.Deriver.Entities;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.Deriver.Utils.CorrelationId;
using SlimMessageBus;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Consumers;

public class ClearanceRequestConsumer(
    ILogger<ClearanceRequestConsumer> logger,
    IDecisionService decisionService,
    ITradeImportsDataApiClient apiClient,
    ICorrelationIdGenerator correlationIdGenerator
) : IConsumer<ResourceEvent<CustomsDeclarationEntity>>, IConsumerWithContext
{
    public async Task OnHandle(ResourceEvent<CustomsDeclarationEntity> message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Received clearance request {ResourceId} of sub type {SubResourceType} with Etag {Etag} and resource version {Version}",
            message.ResourceId,
            message.SubResourceType,
            message.ETag,
            message.Resource?.ClearanceRequest?.GetVersion()
        );

        var clearanceRequest = await apiClient.GetCustomsDeclaration(message.ResourceId, cancellationToken);

        logger.LogInformation(
            "Fetched clearance request {ResourceId} with Etag {Etag} and resource version {Version}",
            message.ResourceId,
            clearanceRequest?.ETag,
            clearanceRequest?.ClearanceRequest.GetVersion()
        );

        if (message.Resource?.ClearanceRequest?.GetVersion() != clearanceRequest?.ClearanceRequest.GetVersion())
        {
            logger.LogInformation(
                message.Resource?.ClearanceRequest?.MessageSentAt.TrimMicroseconds()
                > clearanceRequest?.ClearanceRequest?.MessageSentAt.TrimMicroseconds()
                    ? "ClearanceRequest ResourceEvent version does not match API response : ResourceEvent is newer"
                    : "ClearanceRequest ResourceEvent version does not match API response : API response is newer"
            );
        }

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
            customsDeclaration,
            correlationIdGenerator
        );

        if (!newDecision.IsSameAs(customsDeclaration.ClearanceDecision))
        {
            customsDeclaration.ClearanceDecision = newDecision;

            await apiClient.PutCustomsDeclaration(
                existingCustomsDeclaration.MovementReferenceNumber,
                customsDeclaration,
                existingCustomsDeclaration.ETag,
                cancellationToken
            );
        }
        else
        {
            logger.LogInformation("Decision already exists, not persisting");
        }
    }

    public IConsumerContext Context { get; set; } = null!;

    private static bool WasFinalisedBeforeClearanceRequest(CustomsDeclarationResponse? customsDeclaration)
    {
        return customsDeclaration is { ClearanceRequest: not null, Finalisation: not null }
            && customsDeclaration.ClearanceRequest.MessageSentAt > customsDeclaration.Finalisation.MessageSentAt;
    }
}
