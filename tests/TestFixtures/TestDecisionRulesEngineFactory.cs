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
        .AddSingleton<MissingPartTwoDecisionRule>()
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
        .AddSingleton<UnknownCheckCodeDecisionRule>()
        .AddOptions()
        .Configure<DecisionRulesOptions>(_ => { })
        .AddLogging()
        .BuildServiceProvider();

    public DecisionRulesEngine Get(string? notificationType)
    {
        return new DecisionRulesEngineFactory(sp).Get(notificationType);
    }
}
