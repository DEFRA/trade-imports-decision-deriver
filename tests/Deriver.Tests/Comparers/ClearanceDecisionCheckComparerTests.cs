using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Comparers;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Comparers;

public class ClearanceDecisionCheckComparerTests
{
    [Fact]
    public void HashCodeTest()
    {
        var check = new ClearanceDecisionCheck { CheckCode = "H201", DecisionCode = "C03" };
        var sut = new ClearanceDecisionCheckComparer();

        Action act = () => sut.GetHashCode(check);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void ReferenceEqualsTest()
    {
        var check = new ClearanceDecisionCheck { CheckCode = "H201", DecisionCode = "C03" };
        var sut = new ClearanceDecisionCheckComparer();

        var result = sut.Equals(check, check);

        result.Should().BeTrue();
    }

    [Fact]
    public void FirstItemIsNull()
    {
        var check = new ClearanceDecisionCheck { CheckCode = "H201", DecisionCode = "C03" };
        var sut = new ClearanceDecisionCheckComparer();

        var result = sut.Equals(check, null);

        result.Should().BeFalse();
    }

    [Fact]
    public void SecondItemIsNull()
    {
        var check = new ClearanceDecisionCheck { CheckCode = "H201", DecisionCode = "C03" };
        var sut = new ClearanceDecisionCheckComparer();

        var result = sut.Equals(null, check);

        result.Should().BeFalse();
    }

    [Fact]
    public void ReferenceDifferentButValuesSames()
    {
        var check1 = new ClearanceDecisionCheck { CheckCode = "H201", DecisionCode = "C03" };
        var check2 = new ClearanceDecisionCheck { CheckCode = "H201", DecisionCode = "C03" };
        var sut = new ClearanceDecisionCheckComparer();

        var result = sut.Equals(check1, check2);

        result.Should().BeTrue();
    }

    [Fact]
    public void ReferenceDifferentCheckCodeDifferent()
    {
        var check1 = new ClearanceDecisionCheck { CheckCode = "H201", DecisionCode = "C03" };
        var check2 = new ClearanceDecisionCheck { CheckCode = "H202", DecisionCode = "C03" };
        var sut = new ClearanceDecisionCheckComparer();

        var result = sut.Equals(check1, check2);

        result.Should().BeFalse();
    }

    [Fact]
    public void ReferenceDifferentDecisionCodeDifferent()
    {
        var check1 = new ClearanceDecisionCheck { CheckCode = "H201", DecisionCode = "C03" };
        var check2 = new ClearanceDecisionCheck { CheckCode = "H201", DecisionCode = "C04" };
        var sut = new ClearanceDecisionCheckComparer();

        var result = sut.Equals(check1, check2);

        result.Should().BeFalse();
    }
}