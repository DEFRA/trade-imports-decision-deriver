namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine;

public delegate DecisionResolutionResult DecisionRuleDelegate(DecisionResolutionContext context);

public interface IDecisionRule
{
    DecisionResolutionResult Execute(DecisionResolutionContext context, DecisionRuleDelegate next);
}
