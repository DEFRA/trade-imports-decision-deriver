using Defra.TradeImportsDataApi.Api.Client;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public sealed class CvedpIuuCheckRule(ITradeImportsDataApiClient apiClient) : IuuCheckRuleBase(apiClient)
{
    public override DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        if (!context.CheckCode.IsIuu())
        {
            return next(context);
        }

        var iuuOption = context.Notification.IuuOption;

        return iuuOption is not null ? MakeDecisionFromIuuOption(iuuOption) : MakeDecisionFromIuuCertificate(context);
    }
}
