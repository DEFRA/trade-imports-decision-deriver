using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;
using Microsoft.Extensions.DependencyInjection;

namespace Defra.TradeImportsDecisionDeriver.TestFixtures;

public class TestDecisionRulesEngineFactory : IDecisionRulesEngineFactory
{
    private static readonly IServiceProvider sp = new ServiceCollection()
        .AddSingleton<OrphanCheckCodeDecisionRule>()
        .AddSingleton<UnlinkedNotificationDecisionRule>()
        .AddSingleton<WrongChedTypeDecisionRule>()
        .AddSingleton<TerminalStatusDecisionRule>()
        .AddSingleton<AmendDecisionRule>()
        .AddSingleton<InspectionRequiredDecisionRule>()
        .AddSingleton<CvedaDecisionRule>()
        .AddSingleton<CvedpIuuCheckRule>()
        .AddSingleton<CvedpDecisionRule>()
        .AddSingleton<ChedppDecisionRule>()
        .AddSingleton<CedDecisionRule>()
        .AddSingleton<CommodityCodeDecisionRule>()
        .AddSingleton<CommodityQuantityCheckDecisionRule>()
        .AddSingleton<UnknownChedTypeDecisionRule>()
        .AddSingleton<Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces.TerminalStatusDecisionRule>()
        .AddSingleton<Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces.CvedpDecisionRule>()
        .AddSingleton<Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces.CedDecisionRule>()
        .AddSingleton<Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces.ChedppDecisionRule>()
        .AddSingleton<Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces.CvedaDecisionRule>()
        .AddOptions()
        .Configure<DecisionRulesOptions>(_ => { })
        .AddLogging()
        .BuildServiceProvider();

    public DecisionRulesEngine Get(string source, string? notificationType)
    {
        return new DecisionRulesEngineFactory(sp).Get(source, notificationType);
    }
}
