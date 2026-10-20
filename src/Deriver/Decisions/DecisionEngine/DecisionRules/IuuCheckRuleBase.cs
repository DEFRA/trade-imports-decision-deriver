using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.Iuu;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public abstract class IuuCheckRuleBase(ITradeImportsDataApiClient apiClient) : IDecisionRule
{
    public abstract DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next);

    protected DecisionEngineResult MakeDecisionFromIuuCertificate(DecisionEngineContext context)
    {
        var iuuReferences = (context.Commodity.Documents ?? [])
            .Where(x => x.HasValidIuuDocumentReference())
            .Select(x => x.GetIuuDocumentReferenceIdentifier())
            .Distinct()
            .ToList();

        if (iuuReferences.Count == 0)
        {
            return new DecisionEngineResult(DecisionCode.X00, GetType().Name, DecisionInternalFurtherDetail.E76);
        }

        var iuuNotification = GetIuuNotification(iuuReferences);

        return iuuNotification != null
            ? MakeDecisionFromIuuOption(iuuNotification.IuuOption)
            : new DecisionEngineResult(DecisionCode.X00, GetType().Name, DecisionInternalFurtherDetail.E76);
    }

    protected DecisionEngineResult MakeDecisionFromIuuOption(string? iuuOption) =>
        iuuOption switch
        {
            ControlAuthorityIuuOption.IUUOK => new DecisionEngineResult(DecisionCode.C07, GetType().Name),
            ControlAuthorityIuuOption.IUUNotCompliant => new DecisionEngineResult(DecisionCode.X00, GetType().Name),
            ControlAuthorityIuuOption.IUUNA => new DecisionEngineResult(DecisionCode.C08, GetType().Name),
            _ => new DecisionEngineResult(DecisionCode.H02, GetType().Name, DecisionInternalFurtherDetail.E93),
        };

    private IuuNotification? GetIuuNotification(List<string> iuuReferences)
    {
        foreach (var reference in iuuReferences)
        {
            // usage of .GetAwaiter().GetResult(); is temp until we can refactor the decision engine to be async
            var response = apiClient.GetIuuNotification(reference, CancellationToken.None).GetAwaiter().GetResult();

            if (response?.IuuNotification != null)
            {
                return response.IuuNotification;
            }
        }

        return null;
    }
}
