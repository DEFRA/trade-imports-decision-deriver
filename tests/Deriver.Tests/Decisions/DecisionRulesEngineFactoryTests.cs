using Defra.TradeImportsDataApi.Domain.Ipaffs.Constants;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.TestFixtures;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions;

public class DecisionRulesEngineFactoryTests
{
    [Theory]
    [InlineData(Constants.ChedSource.Traces, ImportNotificationType.Ced)]
    [InlineData(Constants.ChedSource.Traces, ImportNotificationType.Chedpp)]
    [InlineData(Constants.ChedSource.Traces, ImportNotificationType.Cveda)]
    [InlineData(Constants.ChedSource.Traces, ImportNotificationType.Cvedp)]
    [InlineData(Constants.ChedSource.Ipaffs, ImportNotificationType.Ced)]
    [InlineData(Constants.ChedSource.Ipaffs, ImportNotificationType.Chedpp)]
    [InlineData(Constants.ChedSource.Ipaffs, ImportNotificationType.Cveda)]
    [InlineData(Constants.ChedSource.Ipaffs, ImportNotificationType.Cvedp)]
    public void Test(string source, string type)
    {
        var factory = new TestDecisionRulesEngineFactory();

        factory.Get(source, type).Should().NotBeNull();
    }
}
