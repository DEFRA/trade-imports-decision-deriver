using Defra.TradeImportsDataApi.Domain.Ipaffs.Constants;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.Finders;

public class IuuDecisionFinderTests
{
    [Theory]
    [InlineData(ImportNotificationType.Cvedp, ImportNotificationStatus.Submitted, true, "H224")]
    [InlineData(ImportNotificationType.Cvedp, ImportNotificationStatus.Amend, true, "H224")]
    [InlineData(ImportNotificationType.Cvedp, ImportNotificationStatus.InProgress, true, "H224")]
    [InlineData(ImportNotificationType.Cvedp, ImportNotificationStatus.Modify, true, "H224")]
    [InlineData(ImportNotificationType.Cvedp, ImportNotificationStatus.PartiallyRejected, true, "H224")]
    [InlineData(ImportNotificationType.Cvedp, ImportNotificationStatus.Rejected, true, "H224")]
    [InlineData(ImportNotificationType.Cvedp, ImportNotificationStatus.SplitConsignment, true, "H224")]
    [InlineData(ImportNotificationType.Cvedp, ImportNotificationStatus.Validated, true, "H224")]
    [InlineData(ImportNotificationType.Cvedp, ImportNotificationStatus.Submitted, false, "H222")]
    [InlineData(ImportNotificationType.Cvedp, ImportNotificationStatus.Submitted, false, null)]
    [InlineData(ImportNotificationType.Cveda, ImportNotificationStatus.Submitted, false, "H224")]
    [InlineData(ImportNotificationType.Ced, ImportNotificationStatus.Submitted, false, "H224")]
    [InlineData(ImportNotificationType.Chedpp, ImportNotificationStatus.Submitted, false, "H224")]
    [InlineData(null, ImportNotificationStatus.Submitted, false, "H224")]
    public void CanFindDecisionTest(
        string? importNotificationType,
        string notificationStatus,
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
        var sut = new IuuDecisionFinder();

        var result = sut.CanFindDecision(
            notification,
            string.IsNullOrEmpty(checkCode) ? null : new CheckCode { Value = checkCode },
            null
        );

        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(true, ControlAuthorityIuuOption.IUUOK, DecisionCode.C07, null, "IUU Compliant")]
    [InlineData(true, ControlAuthorityIuuOption.IUUNotCompliant, DecisionCode.X00, null, "IUU Not compliant")]
    [InlineData(true, ControlAuthorityIuuOption.IUUNA, DecisionCode.C08, null, "IUU Not applicable")]
    [InlineData(true, null, DecisionCode.X00, null, "IUU Awaiting decision")]
    [InlineData(true, "999", DecisionCode.X00, DecisionInternalFurtherDetail.E95, "IUU Data error")]
    [InlineData(
        false,
        ControlAuthorityIuuOption.IUUOK,
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E94,
        "IUU Data error"
    )]
    public void FindDecisionTest(
        bool iuuCheckRequired,
        string? iuuOption,
        DecisionCode expectedDecisionCode,
        DecisionInternalFurtherDetail? expectedFurtherDetail = null,
        string? expectedDecisionReason = null
    )
    {
        var notification = new DecisionImportPreNotification
        {
            Id = "Test",
            IuuCheckRequired = iuuCheckRequired,
            IuuOption = iuuOption,
            HasPartTwo = true,
        };
        var sut = new IuuDecisionFinder();

        var result = sut.FindDecision(notification, new CheckCode { Value = CheckCode.IuuCheckCode });

        result.DecisionCode.Should().Be(expectedDecisionCode);
        result.InternalDecisionCode.Should().Be(expectedFurtherDetail);
        result.DecisionReason.Should().StartWith(expectedDecisionReason);
        result.CheckCode?.Value.Should().Be(CheckCode.IuuCheckCode);
    }
}
