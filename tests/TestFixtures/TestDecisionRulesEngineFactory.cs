using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine.DecisionRules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

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
        .AddSingleton<CommodityCodeValidationRule>()
        .AddSingleton<CommodityWeightOrQuantityValidationRule>()
        .AddSingleton<UnknownCheckCodeDecisionRule>()
        .AddLogging()
        .BuildServiceProvider();

    public DecisionRulesEngine Get(string? notificationType)
    {
        return new DecisionRulesEngineFactory(sp).Get(notificationType);
    }
}
