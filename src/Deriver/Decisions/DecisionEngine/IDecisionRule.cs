namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;

public delegate DecisionEngineResult DecisionRuleDelegate(DecisionEngineContext context);

public interface IDecisionRule
{
    DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next);
}
