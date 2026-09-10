using System.Net;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules.Traces;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Refit;
using TradeImportsQuantityMgmt.Client.Clients;
using TradeImportsQuantityMgmt.Contract;

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
        .AddSingleton<TracesTerminalStatusDecisionRule>()
        .AddSingleton<TracesCvedpDecisionRule>()
        .AddSingleton<TracesCedDecisionRule>()
        .AddSingleton<TracesChedppDecisionRule>()
        .AddSingleton<TracesCvedaDecisionRule>()
        .AddSingleton<TracesReservationDecisionRule>()
        .AddSingleton(CreateQuantityManagementClient())
        .AddOptions()
        .Configure<DecisionRulesOptions>(c =>
        {
            c.CommodityQuantityCheckDecisionRule = CreateCommodityQuantityCheckDecisionRuleOptions();
        })
        .AddLogging()
        .BuildServiceProvider();

    public DecisionRulesEngine Get(string source, string? notificationType)
    {
        return new DecisionRulesEngineFactory(sp).Get(source, notificationType);
    }

    public static CommodityQuantityCheckDecisionRuleOptions CreateCommodityQuantityCheckDecisionRuleOptions()
    {
        return new CommodityQuantityCheckDecisionRuleOptions
        {
            Scoring = new CommodityQuantityCheckDecisionRuleScoringOptions
            {
                CommodityWeight = 100,
                CheckCodeWeight = 10,
                ChedTypeWeight = 1,
            },
            ComparisonEntries = new List<CommodityQuantityCheckDecisionRuleComparisonEntry>
            {
                new() { ComparisonType = QuantityComparisonType.Weight, UseFallback = true },
                new()
                {
                    ChedType = "CHEDA",
                    CheckCode = "H221",
                    ComparisonType = QuantityComparisonType.Quantity,
                    UseFallback = false,
                },
                new()
                {
                    ChedType = "CHEDA",
                    CheckCode = "H221",
                    CommodityCode = "0106410000", // Bees
                    ComparisonType = QuantityComparisonType.Weight,
                    UseFallback = false,
                },
                new()
                {
                    ChedType = "CHEDA",
                    CheckCode = "H221",
                    CommodityCode = "0106900010", // Frogs fit for Human Consumption
                    ComparisonType = QuantityComparisonType.Weight,
                    UseFallback = false,
                },
            },
        };
    }

    private static IQuantityManagementClient CreateQuantityManagementClient()
    {
        var client = Substitute.For<IQuantityManagementClient>();

        client
            .PutChedReservation(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ChedReservationRequest>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new ApiResponse<ChedDeclarationReservation>(
                    new HttpResponseMessage(HttpStatusCode.OK),
                    new ChedDeclarationReservation { Reserved = [], Consumed = [] },
                    null!,
                    null!
                )
            );

        return client;
    }
}
