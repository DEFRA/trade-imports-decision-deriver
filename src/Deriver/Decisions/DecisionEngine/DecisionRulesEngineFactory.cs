using System.Collections.Concurrent;
using Defra.TradeImportsDataApi.Domain.Ipaffs.Constants;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces;
using Microsoft.Extensions.Options;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;

public interface IDecisionRulesEngineFactory
{
    DecisionRulesEngine Get(string source, string? notificationType);
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

    public DecisionRulesEngine Get(string source, string? notificationType)
    {
        var key = $"{source}_{notificationType ?? "Unknown"}";

        // Use a switch or mapping based on notificationType to resolve the correct set of rules.
        return _cache.GetOrAdd(
            key,
            k =>
            {
                return k switch
                {
                    ImportNotificationType.Cveda => source == "TRACES"
                        ? CreateEngineForTracesCveda(k)
                        : CreateEngineForCveda(k),
                    ImportNotificationType.Cvedp => source == "TRACES"
                        ? CreateEngineForTracesCvedp(k)
                        : CreateEngineForCvedp(k),
                    ImportNotificationType.Chedpp => source == "TRACES"
                        ? CreateEngineForTracesChedpp(k)
                        : CreateEngineForChedpp(k),
                    ImportNotificationType.Ced => source == "TRACES"
                        ? CreateEngineForTracesCed(k)
                        : CreateEngineForCed(k),
                    _ => new DecisionRulesEngine(
                        "Unknown",
                        new List<IDecisionRule> { AddRule<UnknownChedTypeDecisionRule>() },
                        _logger,
                        _options
                    ),
                };
            }
        );
    }

    private DecisionRulesEngine CreateEngineForTracesCveda(string chedType)
    {
        var rules = new List<IDecisionRule>
        {
            AddRule<TracesTerminalStatusDecisionRule>(),
            AddRule<TracesCvedaDecisionRule>(),
        };

        return new DecisionRulesEngine(
            chedType,
            rules,
            serviceProvider.GetRequiredService<ILogger<DecisionRulesEngine>>(),
            _options
        );
    }

    private DecisionRulesEngine CreateEngineForTracesCvedp(string chedType)
    {
        var rules = new List<IDecisionRule>
        {
            AddRule<TracesTerminalStatusDecisionRule>(),
            AddRule<TracesCvedpDecisionRule>(),
        };

        return new DecisionRulesEngine(
            chedType,
            rules,
            serviceProvider.GetRequiredService<ILogger<DecisionRulesEngine>>(),
            _options
        );
    }

    private DecisionRulesEngine CreateEngineForTracesChedpp(string chedType)
    {
        var rules = new List<IDecisionRule>
        {
            AddRule<TracesTerminalStatusDecisionRule>(),
            AddRule<TracesChedppDecisionRule>(),
        };

        return new DecisionRulesEngine(
            chedType,
            rules,
            serviceProvider.GetRequiredService<ILogger<DecisionRulesEngine>>(),
            _options
        );
    }

    private DecisionRulesEngine CreateEngineForTracesCed(string chedType)
    {
        var rules = new List<IDecisionRule>
        {
            AddRule<TracesTerminalStatusDecisionRule>(),
            AddRule<TracesCedDecisionRule>(),
        };

        return new DecisionRulesEngine(
            chedType,
            rules,
            serviceProvider.GetRequiredService<ILogger<DecisionRulesEngine>>(),
            _options
        );
    }

    private DecisionRulesEngine CreateEngineForCveda(string chedType)
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
