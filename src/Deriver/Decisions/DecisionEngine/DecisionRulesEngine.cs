using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Microsoft.Extensions.Options;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;

public sealed class DecisionRulesEngine(
    string chedType,
    IReadOnlyList<IDecisionRule> rules,
    ILogger<DecisionRulesEngine> logger,
    IOptionsMonitor<DecisionRulesOptions> _options
)
{
    private readonly DecisionRuleDelegate _pipeline = BuildRules(
        rules,
        GetDisabledRulesForChed(chedType, _options.CurrentValue)
    );

    public DecisionEngineResult Run(DecisionEngineContext context)
    {
        context.Logger = logger;
        return _pipeline(context);
    }

    private static DecisionRuleDelegate BuildRules(IReadOnlyList<IDecisionRule> rules, HashSet<string> disabledRules)
    {
        DecisionRuleDelegate pipeline = _ => DecisionEngineResult.UnknownDecision;

        for (var i = rules.Count - 1; i >= 0; i--)
        {
            var rule = rules[i];
            var next = pipeline;
            var ruleName = rule.GetType().Name;

            if (disabledRules.Contains(ruleName))
            {
                pipeline = context =>
                {
                    context.Logger?.LogInformation(
                        "Decision rule {Rule} is disabled by configuration for CHED and was skipped.",
                        ruleName
                    );
                    return next(context);
                };
            }
            else
            {
                pipeline = context => rule.Execute(context, next);
            }
        }

        return pipeline;
    }

    private static HashSet<string> GetDisabledRulesForChed(string chedType, DecisionRulesOptions? options)
    {
        if (options?.Cheds != null && options.Cheds.TryGetValue(chedType ?? string.Empty, out var perChed))
        {
            return new HashSet<string>(
                perChed?.DisabledRules ?? Array.Empty<string>(),
                StringComparer.OrdinalIgnoreCase
            );
        }

        return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
