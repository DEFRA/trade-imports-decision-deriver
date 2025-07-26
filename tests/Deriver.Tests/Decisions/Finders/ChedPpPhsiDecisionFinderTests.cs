using Defra.TradeImportsDataApi.Domain.Ipaffs.Constants;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;
using Humanizer;

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
        var sut = new ChedPPDecisionFinder();

        var result = sut.CanFindDecision(
            notification,
            string.IsNullOrEmpty(checkCode) ? null : new CheckCode { Value = checkCode }
        );

        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(ImportNotificationStatus.Amend, DecisionCode.X00, DecisionInternalFurtherDetail.E99)]
    [InlineData(ImportNotificationStatus.Cancelled, DecisionCode.X00, DecisionInternalFurtherDetail.E80)]
    [InlineData(ImportNotificationStatus.Deleted, DecisionCode.X00, DecisionInternalFurtherDetail.E80)]
    [InlineData(ImportNotificationStatus.Draft, DecisionCode.X00, DecisionInternalFurtherDetail.E99)]
    [InlineData(ImportNotificationStatus.InProgress, DecisionCode.H02)]
    [InlineData(ImportNotificationStatus.Submitted, DecisionCode.H02)]
    [InlineData(ImportNotificationStatus.Modify, DecisionCode.X00, DecisionInternalFurtherDetail.E99)]
    [InlineData(ImportNotificationStatus.PartiallyRejected, DecisionCode.H01)]
    [InlineData(ImportNotificationStatus.Rejected, DecisionCode.N02)]
    [InlineData(ImportNotificationStatus.Replaced, DecisionCode.X00, DecisionInternalFurtherDetail.E80)]
    [InlineData(ImportNotificationStatus.SplitConsignment, DecisionCode.X00, DecisionInternalFurtherDetail.E80)]
    public void DecisionFinderTest(
        string status,
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

    [Theory]
    [InlineData("To do", DecisionCode.H01)]
    [InlineData("Hold", DecisionCode.H01)]
    [InlineData("To be inspected", DecisionCode.H02)]
    [InlineData("Compliant", DecisionCode.C03)]
    [InlineData("Auto cleared", DecisionCode.C03)]
    [InlineData("Non compliant", DecisionCode.N01)]
    [InlineData("Not inspected", DecisionCode.C02)]
    [InlineData("invalid", DecisionCode.X00)]
    [InlineData(null, DecisionCode.H01)]
    public void HmiDecisionFinderTest(string? status, DecisionCode expectedCode)
    {
        var notification = new DecisionImportPreNotification
        {
            Id = "Test",
            Status = ImportNotificationStatus.Validated,
        };
        if (!string.IsNullOrEmpty(status))
        {
            notification.CommodityChecks = [new DecisionCommodityCheck.Check() { Type = "HMI", Status = status }];
        }

        var sut = new ChedPPDecisionFinder();

        var result = sut.FindDecision(notification, new CheckCode() { Value = "H218" });

        result.DecisionCode.Should().Be(expectedCode);
    }


    [Theory]
    [InlineData("To do", "Hold", "To be inspected", DecisionCode.H02)]
    [InlineData("Hold", "To be inspected", "Non compliant", DecisionCode.N01)]
    [InlineData("To be inspected", "Non compliant", "Compliant", DecisionCode.N01)]
    [InlineData("Compliant", null, "Compliant", DecisionCode.H01)]
    [InlineData("Compliant", "To do", "Compliant", DecisionCode.H01)]
    [InlineData("Compliant", "Hold", "Compliant", DecisionCode.H01)]
    [InlineData("Compliant", "To be inspected", "Compliant", DecisionCode.H02)]
    [InlineData("Compliant", "Auto cleared", "Compliant", DecisionCode.C03)]
    [InlineData("Compliant", "Non compliant", "Compliant", DecisionCode.N01)]
    [InlineData("Compliant", "Not inspected", "Compliant", DecisionCode.C03)]
    [InlineData("Compliant", "Compliant", null, DecisionCode.H01)]
    [InlineData("Compliant", "Compliant", "To do", DecisionCode.H01)]
    [InlineData("Compliant", "Compliant", "Hold", DecisionCode.H01)]
    [InlineData("Compliant", "Compliant", "To be inspected", DecisionCode.H02)]
    [InlineData("Compliant", "Compliant", "Auto cleared", DecisionCode.C03)]
    [InlineData("Compliant", "Compliant", "Non compliant", DecisionCode.N01)]
    [InlineData("Compliant", "Compliant", "Not inspected", DecisionCode.C03)]
    [InlineData("Non compliant", "Compliant", "Compliant", DecisionCode.N01)]
    [InlineData("Not inspected", null, "To do", DecisionCode.H01)]
    [InlineData("Not inspected", "Not inspected", "Not inspected", DecisionCode.C02)]
    [InlineData("Not inspected", "Compliant", "Compliant", DecisionCode.C03)]
    [InlineData(null, "Compliant", "Compliant", DecisionCode.H01)]
    [InlineData("To do", "Compliant", "Compliant", DecisionCode.H01)]
    [InlineData("Hold", "Compliant", "Compliant", DecisionCode.H01)]
    [InlineData("To be inspected", "Compliant", "Compliant", DecisionCode.H02)]
    [InlineData(null, "To do", "Hold", DecisionCode.H01)]
    [InlineData(null, "Auto cleared", "Auto cleared", DecisionCode.H01)]
    [InlineData("Auto cleared", "Compliant", "Compliant", DecisionCode.C03)]
    [InlineData("Auto cleared", null, "Auto cleared", DecisionCode.H01)]
    [InlineData("Auto cleared", "Auto cleared", null, DecisionCode.H01)]
    // All checks in same state
    [InlineData("To do", "To do", "To do", DecisionCode.H01)]
    [InlineData("Hold", "Hold", "Hold", DecisionCode.H01)]
    [InlineData("To be inspected", "To be inspected", "To be inspected", DecisionCode.H02)]
    [InlineData("Compliant", "Compliant", "Compliant", DecisionCode.C03)]
    [InlineData("Auto cleared", "Auto cleared", "Auto cleared", DecisionCode.C03)]
    [InlineData("Non compliant", "Non compliant", "Non compliant", DecisionCode.N01)]
    [InlineData("invalid", "invalid", "invalid", DecisionCode.X00)]
    [InlineData(null, null, null, DecisionCode.H01)]
    public void PhsiDecisionFinderTest(
        string? documentStatus,
        string? physicalStatus,
        string? identityStatus,
        DecisionCode expectedCode
    )
    {
        var notification = new DecisionImportPreNotification
        {
            Id = "Test",
            Status = ImportNotificationStatus.Validated,
        };
        var checks = new List<DecisionCommodityCheck.Check>();
        if (!string.IsNullOrEmpty(documentStatus))
        {
            checks.Add(new DecisionCommodityCheck.Check() { Type = "PHSI_DOCUMENT", Status = documentStatus });
        }

        if (!string.IsNullOrEmpty(physicalStatus))
        {
            checks.Add(new DecisionCommodityCheck.Check() { Type = "PHSI_PHYSICAL", Status = physicalStatus });
        }

        if (!string.IsNullOrEmpty(identityStatus))
        {
            checks.Add(new DecisionCommodityCheck.Check() { Type = "PHSI_IDENTITY", Status = identityStatus });
        }

        notification.CommodityChecks = checks.ToArray();

        var sut = new ChedPPDecisionFinder();

        var result = sut.FindDecision(notification, new CheckCode() { Value = "H219" });

        Console.WriteLine(result);
        result.DecisionCode.Should().Be(expectedCode);
    }
}
