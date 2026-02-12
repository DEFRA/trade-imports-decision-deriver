using System.Collections.Concurrent;
using Defra.TradeImportsDataApi.Domain.Ipaffs.Constants;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;
using Microsoft.Extensions.Options;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;

public interface IDecisionRulesEngineFactory
{
    DecisionRulesEngine Get(string? notificationType);
}

public sealed class DecisionRulesEngineFactory(IServiceProvider serviceProvider) : IDecisionRulesEngineFactory
{
    private readonly ConcurrentDictionary<string, DecisionRulesEngine> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly IOptionsMonitor<DecisionRulesOptions> _options = serviceProvider.GetRequiredService<
        IOptionsMonitor<DecisionRulesOptions>
    >();
    private readonly ILogger<DecisionRulesEngine> _logger = serviceProvider.GetRequiredService<
        ILogger<DecisionRulesEngine>
    >();

    public DecisionRulesEngine Get(string? notificationType)
    {
        var key = notificationType ?? "Unknown";

        // Use a switch or mapping based on notificationType to resolve the correct set of rules.
        return _cache.GetOrAdd(
            key,
            k =>
            {
                return k switch
                {
                    ImportNotificationType.Cveda => CreateEngineForCveda(k),
                    ImportNotificationType.Cvedp => CreateEngineForCvedp(k),
                    ImportNotificationType.Chedpp => CreateEngineForChedpp(k),
                    ImportNotificationType.Ced => CreateEngineForCed(k),
                    _ => new DecisionRulesEngine(
                        "Unknown",
                        new List<IDecisionRule> { AddRule<UnknownCheckCodeDecisionRule>() },
                        _logger,
                        _options
                    ),
                };
            }
        );
    }

    private DecisionRulesEngine CreateEngineForCveda(string chedType)
    {
        //OrphanCheckCodeDecisionRule
        var rules = new List<IDecisionRule>
        {
            AddRule<CommodityQuantityCheckDecisionRule>(),
            AddRule<CommodityCodeDecisionRule>(),
            AddRule<OrphanCheckCodeDecisionRule>(),
            AddRule<UnlinkedNotificationDecisionRule>(),
            AddRule<WrongChedTypeDecisionRule>(),
            AddRule<TerminalStatusDecisionRule>(),
            AddRule<AmendDecisionRule>(),
            AddRule<MissingPartTwoDecisionRule>(),
            AddRule<InspectionRequiredDecisionRule>(),
            AddRule<CvedaDecisionRule>(),
        };

        return new DecisionRulesEngine(
            chedType,
            rules,
            serviceProvider.GetRequiredService<ILogger<DecisionRulesEngine>>(),
            _options
        );
    }

    private DecisionRulesEngine CreateEngineForCvedp(string chedType)
    {
        var rules = new List<IDecisionRule>
        {
            AddRule<CommodityQuantityCheckDecisionRule>(),
            AddRule<CommodityCodeDecisionRule>(),
            AddRule<OrphanCheckCodeDecisionRule>(),
            AddRule<UnlinkedNotificationDecisionRule>(),
            AddRule<WrongChedTypeDecisionRule>(),
            AddRule<TerminalStatusDecisionRule>(),
            AddRule<AmendDecisionRule>(),
            AddRule<MissingPartTwoDecisionRule>(),
            AddRule<InspectionRequiredDecisionRule>(),
            AddRule<CvedpIuuCheckRule>(),
            AddRule<CvedpDecisionRule>(),
        };

        return new DecisionRulesEngine(
            chedType,
            rules,
            serviceProvider.GetRequiredService<ILogger<DecisionRulesEngine>>(),
            _options
        );
    }

    private DecisionRulesEngine CreateEngineForChedpp(string chedType)
    {
        var rules = new List<IDecisionRule>
        {
            AddRule<CommodityQuantityCheckDecisionRule>(),
            AddRule<CommodityCodeDecisionRule>(),
            AddRule<OrphanCheckCodeDecisionRule>(),
            AddRule<UnlinkedNotificationDecisionRule>(),
            AddRule<WrongChedTypeDecisionRule>(),
            AddRule<TerminalStatusDecisionRule>(),
            AddRule<AmendDecisionRule>(),
            AddRule<MissingPartTwoDecisionRule>(),
            AddRule<ChedppDecisionRule>(),
        };

        return new DecisionRulesEngine(
            chedType,
            rules,
            serviceProvider.GetRequiredService<ILogger<DecisionRulesEngine>>(),
            _options
        );
    }

    private DecisionRulesEngine CreateEngineForCed(string chedType)
    {
        var rules = new List<IDecisionRule>
        {
            AddRule<CommodityQuantityCheckDecisionRule>(),
            AddRule<CommodityCodeDecisionRule>(),
            AddRule<OrphanCheckCodeDecisionRule>(),
            AddRule<UnlinkedNotificationDecisionRule>(),
            AddRule<WrongChedTypeDecisionRule>(),
            AddRule<TerminalStatusDecisionRule>(),
            AddRule<AmendDecisionRule>(),
            AddRule<MissingPartTwoDecisionRule>(),
            AddRule<InspectionRequiredDecisionRule>(),
            AddRule<CedDecisionRule>(),
        };

        return new DecisionRulesEngine(
            chedType,
            rules,
            serviceProvider.GetRequiredService<ILogger<DecisionRulesEngine>>(),
            _options
        );
    }

    private T AddRule<T>()
        where T : notnull
    {
        return serviceProvider.GetRequiredService<T>();
    }
}
