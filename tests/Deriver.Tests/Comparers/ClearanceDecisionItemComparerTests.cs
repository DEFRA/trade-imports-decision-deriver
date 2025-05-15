using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Comparers;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Comparers;

public class ClearanceDecisionItemComparerTests
{
    [Fact]
    public void HashCodeTest()
    {
        var item = new ClearanceDecisionItem
        {
            ItemNumber = 1,
            Checks = [new ClearanceDecisionCheck { CheckCode = "H201", DecisionCode = "C03" }],
        };
        var sut = new ClearanceDecisionItemComparer();

        Action act = () => sut.GetHashCode(item);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void ReferenceEqualsTest()
    {
        var item = new ClearanceDecisionItem
        {
            ItemNumber = 1,
            Checks = [new ClearanceDecisionCheck { CheckCode = "H201", DecisionCode = "C03" }],
        };
        var sut = new ClearanceDecisionItemComparer();

        var result = sut.Equals(item, item);

        result.Should().BeTrue();
    }

    [Fact]
    public void FirstItemIsNull()
    {
        var item = new ClearanceDecisionItem
        {
            ItemNumber = 1,
            Checks = [new ClearanceDecisionCheck { CheckCode = "H201", DecisionCode = "C03" }],
        };
        var sut = new ClearanceDecisionItemComparer();

        var result = sut.Equals(null, item);

        result.Should().BeFalse();
    }

    [Fact]
    public void SecondItemIsNull()
    {
        var item = new ClearanceDecisionItem
        {
            ItemNumber = 1,
            Checks = [new ClearanceDecisionCheck { CheckCode = "H201", DecisionCode = "C03" }],
        };
        var sut = new ClearanceDecisionItemComparer();

        var result = sut.Equals(item, null);

        result.Should().BeFalse();
    }

    [Fact]
    public void ReferenceDifferentButValuesSames()
    {
        var item1 = new ClearanceDecisionItem
        {
            ItemNumber = 1,
            Checks = [new ClearanceDecisionCheck { CheckCode = "H201", DecisionCode = "C03" }],
        };
        var item2 = new ClearanceDecisionItem
        {
            ItemNumber = 1,
            Checks = [new ClearanceDecisionCheck { CheckCode = "H201", DecisionCode = "C03" }],
        };
        var sut = new ClearanceDecisionItemComparer();

        var result = sut.Equals(item1, item2);

        result.Should().BeTrue();
    }

    [Fact]
    public void ReferenceDifferentItemNumberDifferent()
    {
        var item1 = new ClearanceDecisionItem
        {
            ItemNumber = 1,
            Checks = [new ClearanceDecisionCheck { CheckCode = "H201", DecisionCode = "C03" }],
        };
        var item2 = new ClearanceDecisionItem
        {
            ItemNumber = 2,
            Checks = [new ClearanceDecisionCheck { CheckCode = "H201", DecisionCode = "C03" }],
        };
        var sut = new ClearanceDecisionItemComparer();

        var result = sut.Equals(item1, item2);

        result.Should().BeFalse();
    }

    [Fact]
    public void ReferenceDifferentChecksDifferent()
    {
        var item1 = new ClearanceDecisionItem
        {
            ItemNumber = 1,
            Checks = [new ClearanceDecisionCheck { CheckCode = "H201", DecisionCode = "C03" }],
        };
        var item2 = new ClearanceDecisionItem
        {
            ItemNumber = 1,
            Checks = [new ClearanceDecisionCheck { CheckCode = "H202", DecisionCode = "C03" }],
        };
        var sut = new ClearanceDecisionItemComparer();

        var result = sut.Equals(item1, item2);

        result.Should().BeFalse();
    }

    [Fact]
    public void SameChecksDifferentOrder()
    {
        var item1 = new ClearanceDecisionItem
        {
            ItemNumber = 1,
            Checks =
            [
                new ClearanceDecisionCheck { CheckCode = "H201", DecisionCode = "C04" },
                new ClearanceDecisionCheck { CheckCode = "H202", DecisionCode = "C03" },
            ],
        };
        var item2 = new ClearanceDecisionItem
        {
            ItemNumber = 1,
            Checks =
            [
                new ClearanceDecisionCheck { CheckCode = "H202", DecisionCode = "C03" },
                new ClearanceDecisionCheck { CheckCode = "H201", DecisionCode = "C04" },
            ],
        };
        var sut = new ClearanceDecisionItemComparer();

        var result = sut.Equals(item1, item2);

        result.Should().BeTrue();
    }
}