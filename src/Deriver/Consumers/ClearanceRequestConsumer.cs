using System.Text.Json;
using Btms.Business.Services.Decisions;
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
        logger.LogInformation("Received notification: {Message}", JsonSerializer.Serialize(message));

        var apiResponse = await apiClient.GetCustomsDeclarationWithImportPreNotification(
            message.ResourceId,
            cancellationToken
        );

        var preNotifications = new List<ImportPreNotification>();

        if (apiResponse?.ImportPreNotifications is not null)
        {
            preNotifications = apiResponse.ImportPreNotifications.Select(x => x.ImportPreNotification).ToList();
        }

        var decisionContext = new DecisionContext(
            preNotifications,
            [new ClearanceRequestWrapper(message.ResourceId, message.Resource)]
        );
        var decisionResult = await decisionService.Process(decisionContext, Context.CancellationToken);

        logger.LogInformation("Decision Derived: {Decision}", JsonSerializer.Serialize(decisionResult));
    }

    public IConsumerContext Context { get; set; } = null!;
}
