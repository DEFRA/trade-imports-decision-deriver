using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Ipaffs.Constants;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;
using Humanizer;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.Finders;

// ReSharper disable once InconsistentNaming
public class ChedPpPhsiDecisionFinderTests
{
    [Theory]
    [InlineData(ImportNotificationType.Chedpp, "H218", "C085", true)]
    [InlineData(ImportNotificationType.Chedpp, "H218", "N002", true)]
    [InlineData(ImportNotificationType.Chedpp, "H218", "9115", false)]
    [InlineData(ImportNotificationType.Chedpp, "H218", "N853", false)]
    [InlineData(ImportNotificationType.Chedpp, "H218", "C640", false)]
    [InlineData(ImportNotificationType.Chedpp, "H218", "C678", false)]
    [InlineData(ImportNotificationType.Chedpp, "H218", "N852", false)]
    [InlineData(ImportNotificationType.Chedpp, "H218", "C641", false)]
    [InlineData(ImportNotificationType.Chedpp, "H218", "C673", false)]
    [InlineData(ImportNotificationType.Chedpp, "H218", "C674", false)]
    [InlineData(ImportNotificationType.Chedpp, "H219", "C085", true)]
    [InlineData(ImportNotificationType.Chedpp, "H219", "N002", false)]
    [InlineData(ImportNotificationType.Chedpp, "H219", "9115", true)]
    [InlineData(ImportNotificationType.Chedpp, "H219", "N853", false)]
    [InlineData(ImportNotificationType.Chedpp, "H219", "C640", false)]
    [InlineData(ImportNotificationType.Chedpp, "H219", "C678", false)]
    [InlineData(ImportNotificationType.Chedpp, "H219", "N852", false)]
    [InlineData(ImportNotificationType.Chedpp, "H219", "C641", false)]
    [InlineData(ImportNotificationType.Chedpp, "H219", "C673", false)]
    [InlineData(ImportNotificationType.Chedpp, "H219", "C674", false)]
    [InlineData(ImportNotificationType.Chedpp, "H220", "C085", true)]
    [InlineData(ImportNotificationType.Chedpp, "H220", "N002", true)]
    [InlineData(ImportNotificationType.Chedpp, "H220", "9115", false)]
    [InlineData(ImportNotificationType.Chedpp, "H220", "N853", false)]
    [InlineData(ImportNotificationType.Chedpp, "H220", "C640", false)]
    [InlineData(ImportNotificationType.Chedpp, "H220", "C678", false)]
    [InlineData(ImportNotificationType.Chedpp, "H220", "N852", false)]
    [InlineData(ImportNotificationType.Chedpp, "H220", "C641", false)]
    [InlineData(ImportNotificationType.Chedpp, "H220", "C673", false)]
    [InlineData(ImportNotificationType.Chedpp, "H220", "C674", false)]
    [InlineData(ImportNotificationType.Chedpp, "H221", "C085", false)]
    [InlineData(ImportNotificationType.Chedpp, "H221", "N002", false)]
    [InlineData(ImportNotificationType.Chedpp, "H221", "9115", false)]
    [InlineData(ImportNotificationType.Chedpp, "H221", "N853", false)]
    [InlineData(ImportNotificationType.Chedpp, "H221", "C640", true)]
    [InlineData(ImportNotificationType.Chedpp, "H221", "C678", false)]
    [InlineData(ImportNotificationType.Chedpp, "H221", "N852", false)]
    [InlineData(ImportNotificationType.Chedpp, "H221", "C641", false)]
    [InlineData(ImportNotificationType.Chedpp, "H221", "C673", false)]
    [InlineData(ImportNotificationType.Chedpp, "H221", "C674", false)]
    [InlineData(ImportNotificationType.Chedpp, "H222", "C085", false)]
    [InlineData(ImportNotificationType.Chedpp, "H222", "N002", false)]
    [InlineData(ImportNotificationType.Chedpp, "H222", "9115", false)]
    [InlineData(ImportNotificationType.Chedpp, "H222", "N853", true)]
    [InlineData(ImportNotificationType.Chedpp, "H222", "C640", false)]
    [InlineData(ImportNotificationType.Chedpp, "H222", "C678", false)]
    [InlineData(ImportNotificationType.Chedpp, "H222", "N852", false)]
    [InlineData(ImportNotificationType.Chedpp, "H222", "C641", false)]
    [InlineData(ImportNotificationType.Chedpp, "H222", "C673", false)]
    [InlineData(ImportNotificationType.Chedpp, "H222", "C674", false)]
    [InlineData(ImportNotificationType.Chedpp, "H223", "C085", false)]
    [InlineData(ImportNotificationType.Chedpp, "H223", "N002", false)]
    [InlineData(ImportNotificationType.Chedpp, "H223", "9115", false)]
    [InlineData(ImportNotificationType.Chedpp, "H223", "N853", false)]
    [InlineData(ImportNotificationType.Chedpp, "H223", "C640", false)]
    [InlineData(ImportNotificationType.Chedpp, "H223", "C678", true)]
    [InlineData(ImportNotificationType.Chedpp, "H223", "N852", true)]
    [InlineData(ImportNotificationType.Chedpp, "H223", "C641", false)]
    [InlineData(ImportNotificationType.Chedpp, "H223", "C673", false)]
    [InlineData(ImportNotificationType.Chedpp, "H223", "C674", false)]
    [InlineData(ImportNotificationType.Chedpp, "H224", "C085", false)]
    [InlineData(ImportNotificationType.Chedpp, "H224", "N002", false)]
    [InlineData(ImportNotificationType.Chedpp, "H224", "9115", false)]
    [InlineData(ImportNotificationType.Chedpp, "H224", "N853", true)]
    [InlineData(ImportNotificationType.Chedpp, "H224", "C640", false)]
    [InlineData(ImportNotificationType.Chedpp, "H224", "C678", false)]
    [InlineData(ImportNotificationType.Chedpp, "H224", "N852", false)]
    [InlineData(ImportNotificationType.Chedpp, "H224", "C641", false)]
    [InlineData(ImportNotificationType.Chedpp, "H224", "C673", false)]
    [InlineData(ImportNotificationType.Chedpp, "H224", "C674", false)]
    public void CanFindDecisionTest(
        string? importNotificationType,
        string checkCode,
        string documentCode,
        bool expectedResult
    )
    {
        var notification = new DecisionImportPreNotification
        {
            Id = "Test",
            ImportNotificationType = importNotificationType,
        };
        var sut = new ChedPPDecisionFinder();

        var result = sut.CanFindDecision(
            notification,
            string.IsNullOrEmpty(checkCode) ? null : new CheckCode { Value = checkCode },
            documentCode
        );

        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(ImportNotificationStatus.Amend, DecisionCode.H01, DecisionInternalFurtherDetail.E99)]
    [InlineData(ImportNotificationStatus.Cancelled, DecisionCode.X00, DecisionInternalFurtherDetail.E71)]
    [InlineData(ImportNotificationStatus.Deleted, DecisionCode.X00, DecisionInternalFurtherDetail.E73)]
    [InlineData(ImportNotificationStatus.Draft, DecisionCode.X00, DecisionInternalFurtherDetail.E99)]
    [InlineData(ImportNotificationStatus.InProgress, DecisionCode.H02)]
    [InlineData(ImportNotificationStatus.Submitted, DecisionCode.H02)]
    [InlineData(ImportNotificationStatus.Modify, DecisionCode.X00, DecisionInternalFurtherDetail.E99)]
    [InlineData(ImportNotificationStatus.PartiallyRejected, DecisionCode.H01, DecisionInternalFurtherDetail.E74)]
    [InlineData(ImportNotificationStatus.Rejected, DecisionCode.X00, DecisionInternalFurtherDetail.E99)]
    [InlineData(ImportNotificationStatus.Replaced, DecisionCode.X00, DecisionInternalFurtherDetail.E72)]
    [InlineData(ImportNotificationStatus.SplitConsignment, DecisionCode.X00, DecisionInternalFurtherDetail.E75)]
    public void DecisionFinderTest(
        string status,
        DecisionCode expectedCode,
        DecisionInternalFurtherDetail? expectedFurtherDetail = null
    )
    {
        var notification = new DecisionImportPreNotification
        {
            Id = "Test",
            ImportNotificationType = ImportNotificationType.Chedpp,
            Status = status,
            HasPartTwo = true,
        };
        var sut = new ChedPPDecisionFinder();

        var result = sut.FindDecision(notification, new Commodity(), null);

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
            ImportNotificationType = ImportNotificationType.Chedpp,
            Status = ImportNotificationStatus.Validated,
            HasPartTwo = true,
        };
        if (!string.IsNullOrEmpty(status))
        {
            notification.CommodityChecks = [new DecisionCommodityCheck.Check() { Type = "HMI", Status = status }];
        }

        var sut = new ChedPPDecisionFinder();

        var result = sut.FindDecision(notification, new Commodity(), new CheckCode() { Value = "H218" });

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
            ImportNotificationType = ImportNotificationType.Chedpp,
            Status = ImportNotificationStatus.Validated,
            HasPartTwo = true,
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

        var result = sut.FindDecision(notification, new Commodity(), new CheckCode() { Value = "H219" });

        result.DecisionCode.Should().Be(expectedCode);
    }

    [Theory]
    [InlineData(ImportNotificationStatus.Amend)]
    public void WhenInspectionNotRequired_DecisionShouldBeHold(string notificationStatus)
    {
        var notification = new DecisionImportPreNotification
        {
            Id = "TEst",
            ImportNotificationType = ImportNotificationType.Chedpp,
            Status = notificationStatus,
            InspectionRequired = InspectionRequired.NotRequired,
            HasPartTwo = true,
        };
        var sut = new ChedPPDecisionFinder();

        var result = sut.FindDecision(notification, new Commodity(), new CheckCode() { Value = "H221" });

        result.DecisionCode.Should().Be(DecisionCode.H01);
    }

    [Theory]
    [InlineData(ImportNotificationStatus.Amend)]
    public void WhenInspectionRequired_DecisionShouldBeHold(string notificationStatus)
    {
        var notification = new DecisionImportPreNotification
        {
            Id = "TEst",
            ImportNotificationType = ImportNotificationType.Chedpp,
            Status = notificationStatus,
            InspectionRequired = InspectionRequired.Required,
            Commodities = [new DecisionCommodityComplement { HmiDecision = CommodityRiskResultHmiDecision.Required }],
            HasPartTwo = true,
        };
        var sut = new ChedPPDecisionFinder();

        var result = sut.FindDecision(notification, new Commodity(), new CheckCode() { Value = "H221" });

        result.DecisionCode.Should().Be(DecisionCode.H02);
    }

    [Fact]
    public void WhenMissingPartTwo_DecisionShouldBeH01()
    {
        var notification = new DecisionImportPreNotification
        {
            Id = "TEst",
            ImportNotificationType = ImportNotificationType.Chedpp,
            Status = ImportNotificationStatus.Submitted,
        };
        var sut = new ChedPPDecisionFinder();

        var result = sut.FindDecision(notification, new Commodity(), null);

        result.DecisionCode.Should().Be(DecisionCode.H01);
        result.InternalDecisionCode.Should().Be(DecisionInternalFurtherDetail.E88);
    }
}
