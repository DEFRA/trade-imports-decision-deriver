using Defra.TradeImportsDataApi.Api.Client;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces;

public sealed class TracesCvedpIuuCheckRule(ITradeImportsDataApiClient apiClient) : IuuCheckRuleBase(apiClient)
{
    public override DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        if (!context.CheckCode.IsIuu())
        {
            return next(context);
        }

        return MakeDecisionFromIuuCertificate(context);
    }
}
