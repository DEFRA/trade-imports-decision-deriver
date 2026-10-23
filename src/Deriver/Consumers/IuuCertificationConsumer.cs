using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.Events;
using SlimMessageBus;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Consumers;

public class IuuCertificationConsumer(
    ILogger<IuuCertificationConsumer> logger,
    ITradeImportsDataApiClient apiClient,
    ClearanceRequestConsumer clearanceRequestConsumer
) : IConsumer<ResourceEvent<IuuNotificationEvent>>, IConsumerWithContext
{
    public async Task OnHandle(ResourceEvent<IuuNotificationEvent> message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received IUU certification {ResourceId}", message.ResourceId);

        var relatedImportDeclarations = await apiClient.RelatedImportDeclarations(
            new RelatedImportDeclarationsRequest { Iuu = message.ResourceId },
            cancellationToken
        );

        var customsDeclarations = relatedImportDeclarations
            .CustomsDeclarations.Where(x => x.ClearanceRequest is not null)
            .ToList();
        if (customsDeclarations.Count == 0)
        {
            logger.LogInformation("No decision derived, no customs declaration found");

            return;
        }

        clearanceRequestConsumer.Context = Context;

        foreach (var customsDeclaration in customsDeclarations)
        {
            await clearanceRequestConsumer.OnHandle(
                new ResourceEvent<CustomsDeclarationEvent>
                {
                    ResourceId = customsDeclaration.MovementReferenceNumber,
                    ResourceType = ResourceEventResourceTypes.CustomsDeclaration,
                    SubResourceType = ResourceEventSubResourceTypes.ClearanceRequest,
                    Operation = ResourceEventOperations.Updated,
                    Etag = customsDeclaration.ETag,
                    Resource = new CustomsDeclarationEvent
                    {
                        Id = customsDeclaration.MovementReferenceNumber,
                        Etag = customsDeclaration.ETag ?? string.Empty,
                        Created = customsDeclaration.Created,
                        Updated = customsDeclaration.Updated,
                        ClearanceRequest = customsDeclaration.ClearanceRequest,
                        ClearanceDecision = customsDeclaration.ClearanceDecision,
                        Finalisation = customsDeclaration.Finalisation,
                        ExternalErrors = customsDeclaration.ExternalErrors,
                    },
                },
                cancellationToken
            );
        }
    }

    public IConsumerContext Context { get; set; } = null!;
}
