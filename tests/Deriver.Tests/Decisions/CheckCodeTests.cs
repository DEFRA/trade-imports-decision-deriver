using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions;

public class CheckCodeTests
{
    [Theory]
    [InlineData("H221", TracesChedType.Cveda)]
    [InlineData("H223", TracesChedType.Ced)]
    [InlineData("H222", TracesChedType.Cvedp)]
    [InlineData("H224", TracesChedType.Cvedp)]
    [InlineData("H218", TracesChedType.Chedpp)]
    [InlineData("H219", TracesChedType.Chedpp)]
    [InlineData("H220", TracesChedType.Chedpp)]
    [InlineData("9115", null)]
    [InlineData("H999", null)]
    [InlineData("", null)]
    public void GetTracesChedType_ReturnsExpectedChedType(string checkCode, string? expected)
    {
        new CheckCode { Value = checkCode }
            .GetTracesChedType()
            .Should()
            .Be(expected);
    }
}
