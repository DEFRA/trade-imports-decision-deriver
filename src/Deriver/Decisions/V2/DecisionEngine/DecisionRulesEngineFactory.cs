using Defra.TradeImportsDataApi.Domain.Ipaffs.Constants;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine.DecisionRules;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine;

public interface IDecisionRulesEngineFactory
{
    DecisionRulesEngine Get(string? notificationType);
}

public sealed class DecisionRulesEngineFactory(IServiceProvider serviceProvider) : IDecisionRulesEngineFactory
{
    // Inject a dictionary of rules for each notification type, using DI

    public DecisionRulesEngine Get(string? notificationType)
    {
        if (notificationType is null)
        {
            throw new ArgumentNullException(nameof(notificationType));
        }

        // Use a switch or mapping based on notificationType to resolve the correct set of rules.
        return notificationType switch
        {
            ImportNotificationType.Cveda => CreateEngineForCveda(),
            ImportNotificationType.Cvedp => CreateEngineForCvedp(),
            ImportNotificationType.Chedpp => CreateEngineForChedpp(),
            ImportNotificationType.Ced => CreateEngineForCed(),
            _ => throw new ArgumentOutOfRangeException(
                nameof(notificationType),
                notificationType,
                "Unknown import notification type."
            ),
        };
    }

    // Each method uses DI to resolve the dependencies (IDecisionRule) required for the rules engine

    private DecisionRulesEngine CreateEngineForCveda()
    {
        //OrphanCheckCodeDecisionRule
        var rules = new List<IDecisionRule>
        {
            AddRule<OrphanCheckCodeDecisionRule>(),
            AddRule<UnlinkedNotificationDecisionRule>(),
            AddRule<WrongChedTypeDecisionRule>(),
            AddRule<TerminalStatusDecisionRule>(),
            AddRule<MissingPartTwoDecisionRule>(),
            AddRule<AmendDecisionRule>(),
            AddRule<InspectionRequiredDecisionRule>(),
            AddRule<CvedaDecisionRule>(),
            AddRule<CommodityCodeValidationRule>(),
            AddRule<CommodityWeightOrQuantityValidationRule>(),
        };

        return new DecisionRulesEngine(rules, serviceProvider.GetRequiredService<ILogger<DecisionRulesEngine>>());
    }

    private DecisionRulesEngine CreateEngineForCvedp()
    {
        var rules = new List<IDecisionRule>
        {
            AddRule<OrphanCheckCodeDecisionRule>(),
            AddRule<UnlinkedNotificationDecisionRule>(),
            AddRule<WrongChedTypeDecisionRule>(),
            AddRule<TerminalStatusDecisionRule>(),
            AddRule<MissingPartTwoDecisionRule>(),
            AddRule<AmendDecisionRule>(),
            AddRule<InspectionRequiredDecisionRule>(),
            AddRule<CvedpIuuCheckRule>(),
            AddRule<CvedpDecisionRule>(),
            AddRule<CommodityCodeValidationRule>(),
            AddRule<CommodityWeightOrQuantityValidationRule>(),
        };

        return new DecisionRulesEngine(rules, serviceProvider.GetRequiredService<ILogger<DecisionRulesEngine>>());
    }

    private DecisionRulesEngine CreateEngineForChedpp()
    {
        var rules = new List<IDecisionRule>
        {
            AddRule<OrphanCheckCodeDecisionRule>(),
            AddRule<UnlinkedNotificationDecisionRule>(),
            AddRule<WrongChedTypeDecisionRule>(),
            AddRule<TerminalStatusDecisionRule>(),
            AddRule<MissingPartTwoDecisionRule>(),
            AddRule<ChedppDecisionRule>(),
            AddRule<CommodityCodeValidationRule>(),
            AddRule<CommodityWeightOrQuantityValidationRule>(),
        };

        return new DecisionRulesEngine(rules, serviceProvider.GetRequiredService<ILogger<DecisionRulesEngine>>());
    }

    private DecisionRulesEngine CreateEngineForCed()
    {
        var rules = new List<IDecisionRule>
        {
            AddRule<OrphanCheckCodeDecisionRule>(),
            AddRule<UnlinkedNotificationDecisionRule>(),
            AddRule<WrongChedTypeDecisionRule>(),
            AddRule<TerminalStatusDecisionRule>(),
            AddRule<MissingPartTwoDecisionRule>(),
            AddRule<AmendDecisionRule>(),
            AddRule<InspectionRequiredDecisionRule>(),
            AddRule<CedDecisionRule>(),
            AddRule<CommodityCodeValidationRule>(),
            AddRule<CommodityWeightOrQuantityValidationRule>(),
        };

        return new DecisionRulesEngine(rules, serviceProvider.GetRequiredService<ILogger<DecisionRulesEngine>>());
    }

    private T AddRule<T>()
        where T : notnull
    {
        return serviceProvider.GetRequiredService<T>();
    }
}
