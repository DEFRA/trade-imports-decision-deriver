using Defra.TradeImportsDataApi.Domain.Ipaffs.Constants;
using Defra.TradeImportsDecisionDeriver.TestFixtures;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions;

public class DecisionRulesEngineFactoryTests
{
    [Theory]
    [InlineData("TRACES", ImportNotificationType.Ced)]
    [InlineData("TRACES", ImportNotificationType.Chedpp)]
    [InlineData("TRACES", ImportNotificationType.Cveda)]
    [InlineData("TRACES", ImportNotificationType.Cvedp)]
    [InlineData("IPAFFS", ImportNotificationType.Ced)]
    [InlineData("IPAFFS", ImportNotificationType.Chedpp)]
    [InlineData("IPAFFS", ImportNotificationType.Cveda)]
    [InlineData("IPAFFS", ImportNotificationType.Cvedp)]
    public void Test(string source, string type)
    {
        var factory = new TestDecisionRulesEngineFactory();

        factory.Get(source, type).Should().NotBeNull();
    }
}
