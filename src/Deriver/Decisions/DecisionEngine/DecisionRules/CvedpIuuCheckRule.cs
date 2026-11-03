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

        var notification = context.Notification;
        if (notification is { IuuCheckRequired: true, IuuOption: not null })
        {
            return MakeDecisionFromIuuOption(notification.IuuOption);
        }

        return MakeDecisionFromIuuCertificate(context);
    }
}
