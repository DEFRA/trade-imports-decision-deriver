using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Comparers;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Comparers;

public class ClearanceDecisionResultExistsComparerTests
{
    [Fact]
    public void HashCodeTest()
    {
        var item = new ClearanceDecisionResult { ItemNumber = 1 };
        var sut = new ClearanceDecisionResultExistsComparer();

        Action act = () => sut.GetHashCode(item);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void ReferenceEqualsTest()
    {
        var item = new ClearanceDecisionResult { ItemNumber = 1 };
        var sut = new ClearanceDecisionResultExistsComparer();

        var result = sut.Equals(item, item);

        result.Should().BeTrue();
    }

    [Fact]
    public void FirstItemIsNull()
    {
        var item = new ClearanceDecisionResult { ItemNumber = 1 };
        var sut = new ClearanceDecisionResultExistsComparer();

        var result = sut.Equals(null, item);

        result.Should().BeFalse();
    }

    [Fact]
    public void SecondItemIsNull()
    {
        var item = new ClearanceDecisionResult { ItemNumber = 1 };
        var sut = new ClearanceDecisionResultExistsComparer();

        var result = sut.Equals(item, null);

        result.Should().BeFalse();
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
