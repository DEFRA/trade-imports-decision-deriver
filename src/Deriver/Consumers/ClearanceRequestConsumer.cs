using System.Text.Json;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Comparers;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.Processors;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.Deriver.Utils.CorrelationId;
using SlimMessageBus;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Consumers;

public class ClearanceRequestConsumer(
    ILogger<ClearanceRequestConsumer> logger,
    IDecisionService decisionService,
    ITradeImportsDataApiClient apiClient,
    ICorrelationIdGenerator correlationIdGenerator,
    IDecisionServiceV2 decisionServiceV2
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

        var (v1Result, v1Elapsed) = await TimingExtensions.TimeAsync(() =>
            RunV1(message, preNotifications, clearanceRequest, cancellationToken)
        );

        var (v2Result, v2Elapsed) = TimingExtensions.Time(() => RunV2(message, preNotifications, clearanceRequest));

        logger.LogInformation("V1 Took {V1Elapsed} - V2 took {V2Elapsed}", v1Elapsed, v2Elapsed);

        if (!v1Result.ClearanceDecision.IsSameAs(v2Result))
        {
            logger.LogWarning(
                "ClearanceRequestConsumer DecisionResults are different: V1 {V1} - V2 {V2}",
                JsonSerializer.Serialize(v1Result.ClearanceDecision),
                JsonSerializer.Serialize(v2Result)
            );
        }

        if (v1Result.ShouldPersist)
        {
            var customsDeclaration = new CustomsDeclaration
            {
                ClearanceDecision = v1Result.ClearanceDecision,
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

    private ClearanceDecision? RunV2(
        ResourceEvent<CustomsDeclarationEvent> message,
        List<ImportPreNotification> preNotifications,
        CustomsDeclarationResponse? clearanceRequest
    )
    {
        var newResults = decisionServiceV2.Process(
            new DecisionContextV2(
                preNotifications.Select(x => x.ToDecisionImportPreNotification()).ToList(),
                [
                    new CustomsDeclarationWrapper(
                        message.ResourceId,
                        new CustomsDeclaration()
                        {
                            ClearanceDecision = clearanceRequest?.ClearanceDecision,
                            ClearanceRequest = clearanceRequest?.ClearanceRequest,
                        }
                    ),
                ]
            )
        );
        return newResults[0].Decision;
    }

    private async Task<(ClearanceDecision? ClearanceDecision, bool ShouldPersist)> RunV1(
        ResourceEvent<CustomsDeclarationEvent> message,
        List<ImportPreNotification> preNotifications,
        CustomsDeclarationResponse? clearanceRequest,
        CancellationToken cancellationToken
    )
    {
        var decisionContext = new DecisionContext(
            preNotifications.Select(x => x.ToDecisionImportPreNotification()).ToList(),
            [new ClearanceRequestWrapper(message.ResourceId, clearanceRequest!.ClearanceRequest!)]
        );
        var decisionResult = await decisionService.Process(decisionContext, cancellationToken);

        if (!decisionResult.Decisions.Any())
        {
            logger.LogInformation("No decision derived");

            return (null, false);
        }

        logger.LogInformation("Decision derived");

        return BuildDecision(clearanceRequest, decisionResult);
    }

    private (ClearanceDecision ClearanceDecision, bool ShouldPersist) BuildDecision(
        CustomsDeclarationResponse existingCustomsDeclaration,
        DecisionResult decisionResult
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

        var shouldPersist = !newDecision.IsSameAs(existingCustomsDeclaration.ClearanceDecision);
        return (newDecision, shouldPersist);
    }

    public IConsumerContext Context { get; set; } = null!;

    private static bool WasFinalisedBeforeClearanceRequest(CustomsDeclarationResponse? customsDeclaration)
    {
        return customsDeclaration is { ClearanceRequest: not null, Finalisation: not null }
            && customsDeclaration.ClearanceRequest.MessageSentAt > customsDeclaration.Finalisation.MessageSentAt;
    }
}
