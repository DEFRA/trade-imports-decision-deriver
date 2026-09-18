using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public abstract class DecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        var isEu = Region.IsEu(context.ClearanceRequest.CustomsDeclaration.ClearanceRequest?.DispatchCountryCode);

        var disabledRules = isEu
            ? GetDisabledEuRulesForChed(
                context.Source,
                context.CheckCode.GetImportNotificationType(),
                context.DecisionRulesOptions
            )
            : GetDisabledRowRulesForChed(
                context.Source,
                context.CheckCode.GetImportNotificationType(),
                context.DecisionRulesOptions
            );

        var canExecute = !disabledRules.Contains(GetType().Name);

        return canExecute ? DoExecute(context, next) : next(context);
    }

    protected abstract DecisionEngineResult DoExecute(DecisionEngineContext context, DecisionRuleDelegate next);

    private static IEnumerable<string> GetDisabledEuRulesForChed(
        string? source,
        string? chedType,
        DecisionRulesOptions? options
    )
    {
        var sourceOptions = source == Constants.ChedSource.Ipaffs ? options?.Ipaffs : options?.Traces;
        if (sourceOptions?.Cheds != null && sourceOptions.Cheds.TryGetValue(chedType ?? string.Empty, out var perChed))
        {
            return perChed?.DisabledForEu ?? Enumerable.Empty<string>();
        }

        return Enumerable.Empty<string>();
    }

    private static IEnumerable<string> GetDisabledRowRulesForChed(
        string? source,
        string? chedType,
        DecisionRulesOptions? options
    )
    {
        var sourceOptions = source == Constants.ChedSource.Ipaffs ? options?.Ipaffs : options?.Traces;
        if (sourceOptions?.Cheds != null && sourceOptions.Cheds.TryGetValue(chedType ?? string.Empty, out var perChed))
        {
            return perChed?.DisabledForRoW ?? Enumerable.Empty<string>();
        }

        return Enumerable.Empty<string>();
    }
}
