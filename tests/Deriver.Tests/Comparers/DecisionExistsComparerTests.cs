using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Comparers;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Comparers;

public class DecisionExistsComparerTests
{
    [Fact]
    public void HashCodeTest()
    {
        var decision = new ClearanceDecision
        {
            Items =
            [
                new ClearanceDecisionItem
                {
                    ItemNumber = 1,
                    Checks = [new ClearanceDecisionCheck { CheckCode = "H201", DecisionCode = "C03" }],
                },
            ],
        };
        var sut = new DecisionExistsComparer();

        Action act = () => sut.GetHashCode(decision);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void ReferenceEqualsTest()
    {
        var decision = new ClearanceDecision
        {
            Items =
            [
                new ClearanceDecisionItem
                {
                    ItemNumber = 1,
                    Checks = [new ClearanceDecisionCheck { CheckCode = "H201", DecisionCode = "C03" }],
                },
            ],
        };
        var sut = new DecisionExistsComparer();

        var result = sut.Equals(decision, decision);

        result.Should().BeTrue();
    }

    [Fact]
    public void FirstItemIsNull()
    {
        var decision = new ClearanceDecision
        {
            Items =
            [
                new ClearanceDecisionItem
                {
                    ItemNumber = 1,
                    Checks = [new ClearanceDecisionCheck { CheckCode = "H201", DecisionCode = "C03" }],
                },
            ],
        };
        var sut = new DecisionExistsComparer();

        var result = sut.Equals(null, decision);

        result.Should().BeFalse();
    }

    [Fact]
    public void SecondItemIsNull()
    {
        var decision = new ClearanceDecision
        {
            Items =
            [
                new ClearanceDecisionItem
                {
                    ItemNumber = 1,
                    Checks = [new ClearanceDecisionCheck { CheckCode = "H201", DecisionCode = "C03" }],
                },
            ],
        };
        var sut = new DecisionExistsComparer();

        var result = sut.Equals(decision, null);

        result.Should().BeFalse();
    }

    [Fact]
    public void DifferentReferenceButSameValues()
    {
        var decision1 = new ClearanceDecision
        {
            Items =
            [
                new ClearanceDecisionItem
                {
                    ItemNumber = 1,
                    Checks = [new ClearanceDecisionCheck { CheckCode = "H201", DecisionCode = "C03" }],
                },
            ],
        };

        var decision2 = new ClearanceDecision
        {
            Items =
            [
                new ClearanceDecisionItem
                {
                    ItemNumber = 1,
                    Checks = [new ClearanceDecisionCheck { CheckCode = "H201", DecisionCode = "C03" }],
                },
            ],
        };
        var sut = new DecisionExistsComparer();

        var result = sut.Equals(decision1, decision2);

        result.Should().BeTrue();
    }

    [Fact]
    public void DifferentReferenceButSameValuesDifferentOrder()
    {
        var decision1 = new ClearanceDecision
        {
            Items =
            [
                new ClearanceDecisionItem
                {
                    ItemNumber = 1,
                    Checks = [new ClearanceDecisionCheck { CheckCode = "H201", DecisionCode = "C03" }],
                },
                new ClearanceDecisionItem
                {
                    ItemNumber = 2,
                    Checks = [new ClearanceDecisionCheck { CheckCode = "H202", DecisionCode = "C04" }],
                },
            ],
        };

        var decision2 = new ClearanceDecision
        {
            Items =
            [
                new ClearanceDecisionItem
                {
                    ItemNumber = 2,
                    Checks = [new ClearanceDecisionCheck { CheckCode = "H202", DecisionCode = "C04" }],
                },
                new ClearanceDecisionItem
                {
                    ItemNumber = 1,
                    Checks = [new ClearanceDecisionCheck { CheckCode = "H201", DecisionCode = "C03" }],
                },
            ],
        };
        var sut = new DecisionExistsComparer();

        var result = sut.Equals(decision1, decision2);

        result.Should().BeTrue();
    }
}
