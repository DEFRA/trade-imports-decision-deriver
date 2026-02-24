using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Comparers;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Processors;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using SlimMessageBus;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Consumers;

public class ClearanceRequestConsumer(
    ILogger<ClearanceRequestConsumer> logger,
    ITradeImportsDataApiClient apiClient,
    IDecisionService decisionService
) : IConsumer<ResourceEvent<CustomsDeclarationEvent>>, IConsumerWithContext
{
    public async Task OnHandle(ResourceEvent<CustomsDeclarationEvent> message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Received clearance request {ResourceId} of sub type {SubResourceType} with Etag {Etag} and resource version {Version}",
            message.ResourceId,
            message.SubResourceType,
            message.Etag,
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

        var result = RunDecisionService(message, preNotifications, clearanceRequest);

        if (clearanceRequest == null || !clearanceRequest.ClearanceDecision.IsSameAs(result))
        {
            var customsDeclaration = new CustomsDeclaration
            {
                ClearanceDecision = result,
                Finalisation = clearanceRequest?.Finalisation,
                ClearanceRequest = clearanceRequest?.ClearanceRequest,
                ExternalErrors = clearanceRequest?.ExternalErrors,
            };

            await apiClient.PutCustomsDeclaration(
                message.ResourceId,
                customsDeclaration,
                clearanceRequest?.ETag,
                cancellationToken
            );
        }
        else
        {
            logger.LogInformation("Decision already exists, not persisting");
        }
    }

    private ClearanceDecision? RunDecisionService(
        ResourceEvent<CustomsDeclarationEvent> message,
        List<ImportPreNotification> preNotifications,
        CustomsDeclarationResponse? clearanceRequest
    )
    {
        var decisionImportPreNotifications = preNotifications.Select(x => x.ToDecisionImportPreNotification()).ToList();
        CustomsDeclarationWrapper[] cds =
        [
            new CustomsDeclarationWrapper(
                message.ResourceId,
                new CustomsDeclaration()
                {
                    ClearanceDecision = clearanceRequest?.ClearanceDecision,
                    ClearanceRequest = clearanceRequest?.ClearanceRequest,
                }
            ),
        ];
        var context = new DecisionContext(decisionImportPreNotifications, cds.ToList());

        var newResults = decisionService.Process(context);
        return newResults[0].Decision;
    }

    public IConsumerContext Context { get; set; } = null!;

    private static bool WasFinalisedBeforeClearanceRequest(CustomsDeclarationResponse? customsDeclaration)
    {
        return customsDeclaration is { ClearanceRequest: not null, Finalisation: not null }
            && customsDeclaration.ClearanceRequest.MessageSentAt > customsDeclaration.Finalisation.MessageSentAt;
    }
}
