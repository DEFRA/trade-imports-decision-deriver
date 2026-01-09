namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine;

public delegate DecisionEngineResult DecisionRuleDelegate(DecisionResolutionContext context);

public interface IDecisionRule
{
    DecisionEngineResult Execute(DecisionResolutionContext context, DecisionRuleDelegate next);
}
