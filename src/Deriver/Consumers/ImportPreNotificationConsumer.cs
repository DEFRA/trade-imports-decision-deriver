using System.Text.Json;
using Btms.Business.Services.Decisions;
using Defra.TradeImportsDataApi.Api.Client;
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
) : IConsumer<ResourceEvent<ImportPreNotification>>, IConsumerWithContext
{
    public async Task OnHandle(ResourceEvent<ImportPreNotification> message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received notification: {Message}", JsonSerializer.Serialize(message));
        var apiResponse = await apiClient.GetCustomsDeclarationsByChedId(message.ResourceId, cancellationToken);
        var clearanceRequests = apiResponse
            ?.Where(x => x.ClearanceRequest is not null)
            .Select(x => new ClearanceRequestWrapper(x.MovementReferenceNumber, x.ClearanceRequest!))
            .ToList();

        if (clearanceRequests != null && clearanceRequests.Any())
        {
            var decisionContext = new DecisionContext([message.Resource], new List<ClearanceRequestWrapper>());
            var decisionResult = await decisionService.Process(decisionContext, Context.CancellationToken);
            logger.LogInformation("Decision Derived: {Decision}", JsonSerializer.Serialize(decisionResult));
        }

        logger.LogInformation(
            "No Decision Derived, because no Customs Declaration found for {ChedId}",
            message.ResourceId
        );
    }

    public IConsumerContext Context { get; set; } = null!;
}
