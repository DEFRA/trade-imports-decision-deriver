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
) : IConsumer<ResourceEvent<ClearanceRequest>>, IConsumerWithContext
{
    public async Task OnHandle(ResourceEvent<ClearanceRequest> message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Received notification: {ResourceType}:{ResourceId}",
            message.ResourceType,
            message.ResourceId
        );

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
            preNotifications,
            [new ClearanceRequestWrapper(message.ResourceId, clearanceRequest!.ClearanceRequest!)]
        );
        var decisionResult = await decisionService.Process(decisionContext, Context.CancellationToken);

        logger.LogInformation("Decision Derived: {Decision}", JsonSerializer.Serialize(decisionResult));
    }

    public IConsumerContext Context { get; set; } = null!;
}
