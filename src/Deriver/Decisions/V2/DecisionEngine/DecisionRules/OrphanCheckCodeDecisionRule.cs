using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine.DecisionRules;

public sealed class OrphanCheckCodeDecisionRule : IDecisionRule
{
    private static readonly CommodityCheck[] s_emptyChecks = Array.Empty<CommodityCheck>();

    public DecisionResolutionResult Execute(DecisionResolutionContext context, DecisionRuleDelegate next)
    {
        if (context.ImportDocument is not null)
            return next(context);
        var internalFurtherDetail = DecisionInternalFurtherDetail.E83;
        if (context.CheckCode.Value == "H220")
        {
            var checks = context.Commodity.Checks ?? s_emptyChecks;
            bool hasH219 = checks.Any(t => t.CheckCode == "H219");

            internalFurtherDetail = hasH219 ? DecisionInternalFurtherDetail.E82 : DecisionInternalFurtherDetail.E87;
        }
        return new DecisionResolutionResult(DecisionCode.X00, internalFurtherDetail);
    }
}
