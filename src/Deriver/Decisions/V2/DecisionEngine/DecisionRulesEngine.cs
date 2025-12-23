namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine;

public sealed class DecisionRulesEngine(IReadOnlyList<IDecisionRule> rules, ILogger<DecisionRulesEngine> logger)
{
    public DecisionResolutionResult Resolve(DecisionResolutionContext context)
    {
        var pipeline = BuildRules(rules);
        context.Logger = logger;
        return pipeline(context);
    }

    private static DecisionRuleDelegate BuildRules(IReadOnlyList<IDecisionRule> rules)
    {
        DecisionRuleDelegate pipeline = _ => DecisionResolutionResult.UnknownDecision;

        for (int i = rules.Count - 1; i >= 0; i--)
        {
            var rule = rules[i];
            var next = pipeline;
            pipeline = context => rule.Execute(context, next);
        }

        return pipeline;
    }
}
