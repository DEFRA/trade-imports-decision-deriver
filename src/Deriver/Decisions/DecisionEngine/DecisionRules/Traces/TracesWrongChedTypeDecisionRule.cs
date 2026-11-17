namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces;

public sealed class TracesWrongChedTypeDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        var chedType = context.Ched?.ExchangedDocument.Identifier.Split(".").FirstOrDefault();
        if (chedType != context.CheckCode.GetTracesChedType())
        {
            return new DecisionEngineResult(
                DecisionCode.X00,
                nameof(WrongChedTypeDecisionRule),
                DecisionInternalFurtherDetail.E84
            );
        }

        return next(context);
    }

    
}


