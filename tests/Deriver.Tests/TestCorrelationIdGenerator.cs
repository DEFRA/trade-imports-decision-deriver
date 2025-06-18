using Defra.TradeImportsDecisionDeriver.Deriver.Utils.CorrelationId;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests
{
    internal class TestCorrelationIdGenerator(string value) : ICorrelationIdGenerator
    {
        public string Generate()
        {
            return value;
        }
    }
}
