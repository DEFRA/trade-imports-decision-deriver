using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Comparers;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Comparers;

public class ClearanceDecisionExtensionsTests
{
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
                new ClearanceDecisionItem
                {
                    ItemNumber = 2,
                    Checks = [new ClearanceDecisionCheck { CheckCode = "H202", DecisionCode = "C04" }],
                },
            ],
            Results =
            [
                new ClearanceDecisionResult
                {
                    ItemNumber = 1,
                    DecisionCode = "C03",
                    DocumentReference = "docRef",
                    CheckCode = "checkCode",
                    DecisionReason = "Reason",
                    ImportPreNotification = "notification",
                    InternalDecisionCode = "E99",
                },
            ],
        };

        var result = decision.IsSameAs(decision);

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
                new ClearanceDecisionItem
                {
                    ItemNumber = 2,
                    Checks = [new ClearanceDecisionCheck { CheckCode = "H202", DecisionCode = "C04" }],
                },
            ],
            Results =
            [
                new ClearanceDecisionResult
                {
                    ItemNumber = 1,
                    DecisionCode = "C03",
                    DocumentReference = "docRef",
                    CheckCode = "checkCode",
                    DecisionReason = "Reason",
                    ImportPreNotification = "notification",
                    InternalDecisionCode = "E99",
                },
            ],
        };

        ClearanceDecision? nullDecision = null;
        var result = nullDecision.IsSameAs(decision);

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
                new ClearanceDecisionItem
                {
                    ItemNumber = 2,
                    Checks = [new ClearanceDecisionCheck { CheckCode = "H202", DecisionCode = "C04" }],
                },
            ],
            Results =
            [
                new ClearanceDecisionResult
                {
                    ItemNumber = 1,
                    DecisionCode = "C03",
                    DocumentReference = "docRef",
                    CheckCode = "checkCode",
                    DecisionReason = "Reason",
                    ImportPreNotification = "notification",
                    InternalDecisionCode = "E99",
                },
            ],
        };

        var result = decision.IsSameAs(null);

        result.Should().BeFalse();
    }

    [Fact]
    public void BothResultsAreNull()
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
                new ClearanceDecisionItem
                {
                    ItemNumber = 2,
                    Checks = [new ClearanceDecisionCheck { CheckCode = "H202", DecisionCode = "C04" }],
                },
            ],
            Results = null,
        };

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
            Results = null,
        };

        var result = decision.IsSameAs(decision1);

        result.Should().BeTrue();
    }

    [Fact]
    public void FirstResultsAreNull()
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
                new ClearanceDecisionItem
                {
                    ItemNumber = 2,
                    Checks = [new ClearanceDecisionCheck { CheckCode = "H202", DecisionCode = "C04" }],
                },
            ],
        };

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
            Results =
            [
                new ClearanceDecisionResult
                {
                    ItemNumber = 1,
                    DecisionCode = "C03",
                    DocumentReference = "docRef",
                    CheckCode = "checkCode",
                    DecisionReason = "Reason",
                    ImportPreNotification = "notification",
                    InternalDecisionCode = "E99",
                },
            ],
        };

        var result = decision.IsSameAs(decision1);

        result.Should().BeFalse();
    }

    [Fact]
    public void SecondResultsAreNull()
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
                new ClearanceDecisionItem
                {
                    ItemNumber = 2,
                    Checks = [new ClearanceDecisionCheck { CheckCode = "H202", DecisionCode = "C04" }],
                },
            ],
            Results =
            [
                new ClearanceDecisionResult
                {
                    ItemNumber = 1,
                    DecisionCode = "C03",
                    DocumentReference = "docRef",
                    CheckCode = "checkCode",
                    DecisionReason = "Reason",
                    ImportPreNotification = "notification",
                    InternalDecisionCode = "E99",
                },
            ],
        };

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

        var result = decision.IsSameAs(decision1);

        result.Should().BeFalse();
    }

    [Fact]
    public void ResultsAreSameButDifferentOrder()
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
                new ClearanceDecisionItem
                {
                    ItemNumber = 2,
                    Checks = [new ClearanceDecisionCheck { CheckCode = "H202", DecisionCode = "C04" }],
                },
            ],
            Results =
            [
                new ClearanceDecisionResult
                {
                    ItemNumber = 1,
                    DecisionCode = "decisionCode1",
                    DocumentReference = "docRef1",
                    CheckCode = "checkCode1",
                    DecisionReason = "decisionReason1",
                    ImportPreNotification = "notification1",
                    InternalDecisionCode = "internalDecisionCode1",
                },
                new ClearanceDecisionResult
                {
                    ItemNumber = 2,
                    DecisionCode = "decisionCode2",
                    DocumentReference = "docRef2",
                    CheckCode = "checkCode2",
                    DecisionReason = "decisionReason2",
                    ImportPreNotification = "notification2",
                    InternalDecisionCode = "internalDecisionCode2",
                },
            ],
        };

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
            Results =
            [
                new ClearanceDecisionResult
                {
                    ItemNumber = 2,
                    DecisionCode = "decisionCode2",
                    DocumentCode = "docCode2",
                    DocumentReference = "docRef2",
                    CheckCode = "checkCode2",
                    DecisionReason = "decisionReason2",
                    ImportPreNotification = "notification2",
                    InternalDecisionCode = "internalDecisionCode2",
                },
                new ClearanceDecisionResult
                {
                    ItemNumber = 1,
                    DecisionCode = "decisionCode1",
                    DocumentCode = "docCode1",
                    DocumentReference = "docRef1",
                    CheckCode = "checkCode1",
                    DecisionReason = "decisionReason1",
                    ImportPreNotification = "notification1",
                    InternalDecisionCode = "internalDecisionCode1",
                },
            ],
        };

        var result = decision.IsSameAs(decision1);

        result.Should().BeTrue();
    }

    [Fact]
    public void ReferenceDifferentButValuesSames()
    {
        var item1 = new ClearanceDecisionResult
        {
            ItemNumber = 1,
            DecisionCode = "C03",
            DocumentReference = "docRef",
            CheckCode = "checkCode",
            DecisionReason = "Reason",
            ImportPreNotification = "notification",
            InternalDecisionCode = "E99",
        };
        var item2 = new ClearanceDecisionResult
        {
            ItemNumber = 1,
            DecisionCode = "C03",
            DocumentReference = "docRef",
            CheckCode = "checkCode",
            DecisionReason = "Reason",
            ImportPreNotification = "notification",
            InternalDecisionCode = "E99",
        };
        var sut = new ClearanceDecisionResultExistsComparer();

        var result = sut.Equals(item1, item2);

        result.Should().BeTrue();
    }

    [Fact]
    public void ReferenceDifferentItemNumberDifferent()
    {
        var item1 = new ClearanceDecisionResult
        {
            ItemNumber = 1,
            DecisionCode = "C03",
            DocumentReference = "docRef",
            CheckCode = "checkCode",
            DecisionReason = "Reason",
            ImportPreNotification = "notification",
            InternalDecisionCode = "E99",
        };
        var item2 = new ClearanceDecisionResult
        {
            ItemNumber = 2,
            DecisionCode = "C03",
            DocumentReference = "docRef",
            CheckCode = "checkCode",
            DecisionReason = "Reason",
            ImportPreNotification = "notification",
            InternalDecisionCode = "E99",
        };
        var sut = new ClearanceDecisionResultExistsComparer();

        var result = sut.Equals(item1, item2);

        result.Should().BeFalse();
    }

    [Fact]
    public void ReferenceDifferentDecisionCodeDifferent()
    {
        var item1 = new ClearanceDecisionResult
        {
            ItemNumber = 1,
            DecisionCode = "C03",
            DocumentReference = "docRef",
            CheckCode = "checkCode",
            DecisionReason = "Reason",
            ImportPreNotification = "notification",
            InternalDecisionCode = "E99",
        };
        var item2 = new ClearanceDecisionResult
        {
            ItemNumber = 1,
            DecisionCode = "X00",
            DocumentReference = "docRef",
            CheckCode = "checkCode",
            DecisionReason = "Reason",
            ImportPreNotification = "notification",
            InternalDecisionCode = "E99",
        };
        var sut = new ClearanceDecisionResultExistsComparer();

        var result = sut.Equals(item1, item2);

        result.Should().BeFalse();
    }

    [Fact]
    public void ReferenceDifferentDocumentReferenceDifferent()
    {
        var item1 = new ClearanceDecisionResult
        {
            ItemNumber = 1,
            DecisionCode = "C03",
            DocumentReference = "docRef",
            CheckCode = "checkCode",
            DecisionReason = "Reason",
            ImportPreNotification = "notification",
            InternalDecisionCode = "E99",
        };
        var item2 = new ClearanceDecisionResult
        {
            ItemNumber = 1,
            DecisionCode = "C03",
            DocumentReference = "docRef2",
            CheckCode = "checkCode",
            DecisionReason = "Reason",
            ImportPreNotification = "notification",
            InternalDecisionCode = "E99",
        };
        var sut = new ClearanceDecisionResultExistsComparer();

        var result = sut.Equals(item1, item2);

        result.Should().BeFalse();
    }

    [Fact]
    public void ReferenceDifferentCheckCodeDifferent()
    {
        var item1 = new ClearanceDecisionResult
        {
            ItemNumber = 1,
            DecisionCode = "C03",
            DocumentReference = "docRef",
            CheckCode = "checkCode",
            DecisionReason = "Reason",
            ImportPreNotification = "notification",
            InternalDecisionCode = "E99",
        };
        var item2 = new ClearanceDecisionResult
        {
            ItemNumber = 1,
            DecisionCode = "C03",
            DocumentReference = "docRef",
            CheckCode = "checkCode2",
            DecisionReason = "Reason",
            ImportPreNotification = "notification",
            InternalDecisionCode = "E99",
        };
        var sut = new ClearanceDecisionResultExistsComparer();

        var result = sut.Equals(item1, item2);

        result.Should().BeFalse();
    }

    [Fact]
    public void ReferenceDifferentDecisionReasonDifferent()
    {
        var item1 = new ClearanceDecisionResult
        {
            ItemNumber = 1,
            DecisionCode = "C03",
            DocumentReference = "docRef",
            CheckCode = "checkCode",
            DecisionReason = "Reason",
            ImportPreNotification = "notification",
            InternalDecisionCode = "E99",
        };
        var item2 = new ClearanceDecisionResult
        {
            ItemNumber = 1,
            DecisionCode = "C03",
            DocumentReference = "docRef",
            CheckCode = "checkCode",
            DecisionReason = "Reason2",
            ImportPreNotification = "notification",
            InternalDecisionCode = "E99",
        };
        var sut = new ClearanceDecisionResultExistsComparer();

        var result = sut.Equals(item1, item2);

        result.Should().BeFalse();
    }

    [Fact]
    public void ReferenceDifferentImportPreNotificationDifferent()
    {
        var item1 = new ClearanceDecisionResult
        {
            ItemNumber = 1,
            DecisionCode = "C03",
            DocumentReference = "docRef",
            CheckCode = "checkCode",
            DecisionReason = "Reason",
            ImportPreNotification = "notification",
            InternalDecisionCode = "E99",
        };
        var item2 = new ClearanceDecisionResult
        {
            ItemNumber = 1,
            DecisionCode = "C03",
            DocumentReference = "docRef",
            CheckCode = "checkCode",
            DecisionReason = "Reason",
            ImportPreNotification = "notification2",
            InternalDecisionCode = "E99",
        };
        var sut = new ClearanceDecisionResultExistsComparer();

        var result = sut.Equals(item1, item2);

        result.Should().BeFalse();
    }

    [Fact]
    public void ReferenceDifferentInternalDecisionCodeDifferent()
    {
        var item1 = new ClearanceDecisionResult
        {
            ItemNumber = 1,
            DecisionCode = "C03",
            DocumentReference = "docRef",
            CheckCode = "checkCode",
            DecisionReason = "Reason",
            ImportPreNotification = "notification",
            InternalDecisionCode = "E99",
        };
        var item2 = new ClearanceDecisionResult
        {
            ItemNumber = 1,
            DecisionCode = "C03",
            DocumentReference = "docRef",
            CheckCode = "checkCode",
            DecisionReason = "Reason",
            ImportPreNotification = "notification",
            InternalDecisionCode = "E999",
        };
        var sut = new ClearanceDecisionResultExistsComparer();

        var result = sut.Equals(item1, item2);

        result.Should().BeFalse();
    }
}
