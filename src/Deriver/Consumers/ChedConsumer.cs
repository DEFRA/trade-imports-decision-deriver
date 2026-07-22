using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.Events;
using SlimMessageBus;
using Trade.Gateway.Api.Contract.Certificate;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Consumers;

public abstract class ChedConsumer<TEvent>(ITradeImportsDataApiClient apiClient)
    : IConsumer<ResourceEvent<TEvent>>,
        IConsumerWithContext
{
    protected ITradeImportsDataApiClient ApiClient { get; } = apiClient;

    public abstract Task OnHandle(ResourceEvent<TEvent> message, CancellationToken cancellationToken);

    protected async Task<List<DefraUNVTDCHEDProfile>> GetTracesCheds(string[] mrns)
    {
        var cheds = new List<DefraUNVTDCHEDProfile>();

        await Parallel.ForEachAsync(
            mrns,
            async (mrn, cancellationToken) =>
            {
                var apiResponse = await ApiClient.GetTracesChedsByMrn(mrn, cancellationToken);

                foreach (
                    var chedResponse in apiResponse
                        .Cheds.Where(notificationResponse =>
                            !cheds.Exists(x =>
                                x.ExchangedDocument.Identifier == notificationResponse.Ched.ExchangedDocument.Identifier
                            )
                        )
                        .Select(x => x.Ched)
                )
                {
                    cheds.Add(chedResponse);
                }
            }
        );

        return cheds;
    }

    public IConsumerContext Context { get; set; } = null!;
}
