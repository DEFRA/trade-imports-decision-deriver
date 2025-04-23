using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.Finders;

// ReSharper disable once InconsistentNaming
public class ChedPpPhsiDecisionFinderTests
{
    [Theory]
    [InlineData(ImportNotificationType.Chedpp, ImportNotificationStatus.Submitted, true, "H220")]
    [InlineData(ImportNotificationType.Chedpp, ImportNotificationStatus.Amend, true, "H220")]
    [InlineData(ImportNotificationType.Chedpp, ImportNotificationStatus.InProgress, true, "H220")]
    [InlineData(ImportNotificationType.Chedpp, ImportNotificationStatus.Modify, true, "H220")]
    [InlineData(ImportNotificationType.Chedpp, ImportNotificationStatus.PartiallyRejected, true, "H220")]
    [InlineData(ImportNotificationType.Chedpp, ImportNotificationStatus.Rejected, true, "H220")]
    [InlineData(ImportNotificationType.Chedpp, ImportNotificationStatus.SplitConsignment, true, "H220")]
    [InlineData(ImportNotificationType.Chedpp, ImportNotificationStatus.Validated, true, "H220")]
    [InlineData(ImportNotificationType.Cveda, ImportNotificationStatus.Submitted, false, "H219")]
    [InlineData(ImportNotificationType.Ced, ImportNotificationStatus.Submitted, false, "H219")]
    [InlineData(ImportNotificationType.Cvedp, ImportNotificationStatus.Submitted, false, "H219")]
    [InlineData(ImportNotificationType.Chedpp, ImportNotificationStatus.Submitted, true, "H219")]
    [InlineData(ImportNotificationType.Chedpp, ImportNotificationStatus.Submitted, true, "H218")]
    [InlineData(ImportNotificationType.Chedpp, ImportNotificationStatus.Submitted, false, null)]
    public void CanFindDecisionTest(
        ImportNotificationType? importNotificationType,
        ImportNotificationStatus notificationStatus,
        bool expectedResult,
        string? checkCode
    )
    {
        var notification = new DecisionImportPreNotification
        {
            Id = "Test",
            Status = notificationStatus,
            ImportNotificationType = importNotificationType,
        };
        var sut = new ChedPPDecisionFinder();

        var result = sut.CanFindDecision(
            notification,
            string.IsNullOrEmpty(checkCode) ? null : new CheckCode() { Value = checkCode }
        );

        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(ImportNotificationStatus.Amend, DecisionCode.X00, DecisionInternalFurtherDetail.E99)]
    [InlineData(ImportNotificationStatus.Cancelled, DecisionCode.X00, DecisionInternalFurtherDetail.E88)]
    [InlineData(ImportNotificationStatus.Deleted, DecisionCode.X00, DecisionInternalFurtherDetail.E88)]
    [InlineData(ImportNotificationStatus.Draft, DecisionCode.X00, DecisionInternalFurtherDetail.E99)]
    [InlineData(ImportNotificationStatus.InProgress, DecisionCode.H02)]
    [InlineData(ImportNotificationStatus.Submitted, DecisionCode.H02)]
    [InlineData(ImportNotificationStatus.Modify, DecisionCode.X00, DecisionInternalFurtherDetail.E99)]
    [InlineData(ImportNotificationStatus.PartiallyRejected, DecisionCode.H01)]
    [InlineData(ImportNotificationStatus.Rejected, DecisionCode.N02)]
    [InlineData(ImportNotificationStatus.Replaced, DecisionCode.X00, DecisionInternalFurtherDetail.E88)]
    [InlineData(ImportNotificationStatus.SplitConsignment, DecisionCode.X00, DecisionInternalFurtherDetail.E99)]
    public void DecisionFinderTest(
        ImportNotificationStatus status,
        DecisionCode expectedCode,
        DecisionInternalFurtherDetail? expectedFurtherDetail = null
    )
    {
        var notification = new DecisionImportPreNotification { Id = "Test", Status = status };
        var sut = new ChedPPDecisionFinder();

        var result = sut.FindDecision(notification, null);

        result.DecisionCode.Should().Be(expectedCode);
        result.InternalDecisionCode.Should().Be(expectedFurtherDetail);
        result.CheckCode.Should().BeNull();
    }
}
