using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public sealed class OrphanCheckCodeDecisionRule : IDecisionRule
{
    private static readonly CommodityCheck[] s_emptyChecks = Array.Empty<CommodityCheck>();

    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        if (context.ImportDocument is not null)
            return next(context);
        var internalFurtherDetail = DecisionInternalFurtherDetail.E83;
        if (context.CheckCode.Value == "H220")
        {
            var checks = context.Commodity.Checks ?? s_emptyChecks;
            var hasH219 = checks.Any(t => t.CheckCode == "H219");

            internalFurtherDetail = hasH219 ? DecisionInternalFurtherDetail.E82 : DecisionInternalFurtherDetail.E87;
        }
        return DecisionEngineResult.Create(DecisionCode.X00, internalFurtherDetail);
    }
}
