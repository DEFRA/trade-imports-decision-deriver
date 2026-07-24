using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Comparers;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;
using SlimMessageBus;
using Trade.Gateway.Api.Contract.Certificate;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Consumers;

public abstract class ChedConsumer<TEvent>(ITradeImportsDataApiClient apiClient, ILogger logger)
    : IConsumer<ResourceEvent<TEvent>>,
        IConsumerWithContext
{
    private readonly Lock _lock = new();

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
                    lock (_lock)
                    {
                        cheds.Add(chedResponse);
                    }
                }
            }
        );

        return cheds;
    }

    protected async Task ProcessDecisionResult(
        CancellationToken cancellationToken,
        IReadOnlyList<(string Mrn, ClearanceDecision Decision)> decisionResults
    )
    {
        foreach (var result in decisionResults)
        {
            var existingCustomsDeclaration = await ApiClient.GetCustomsDeclaration(result.Mrn, cancellationToken);

            logger.LogInformation(
                "Fetched clearance request {ResourceId} with Etag {Etag} and resource version {Version}",
                result.Mrn,
                existingCustomsDeclaration?.ETag,
                existingCustomsDeclaration?.ClearanceRequest.GetVersion()
            );

            var customsDeclaration = new CustomsDeclaration
            {
                ClearanceDecision = existingCustomsDeclaration?.ClearanceDecision,
                Finalisation = existingCustomsDeclaration?.Finalisation,
                ClearanceRequest = existingCustomsDeclaration?.ClearanceRequest,
                ExternalErrors = existingCustomsDeclaration?.ExternalErrors,
            };

            if (!result.Decision.IsSameAs(customsDeclaration.ClearanceDecision))
            {
                customsDeclaration.ClearanceDecision = result.Decision;

                await ApiClient.PutCustomsDeclaration(
                    result.Mrn,
                    customsDeclaration,
                    existingCustomsDeclaration?.ETag,
                    cancellationToken
                );
            }
            else
            {
                logger.LogInformation("Decision already exists, not persisting");
            }
        }
    }

    public IConsumerContext Context { get; set; } = null!;
}
