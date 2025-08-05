using Defra.TradeImportsDataApi.Domain.Ipaffs.Constants;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.Finders;

public class ChedADecisionFinderTests
{
    [Theory]
    [InlineData(null, ImportNotificationType.Cveda, ImportNotificationStatus.Submitted, true)]
    [InlineData(null, ImportNotificationType.Cveda, ImportNotificationStatus.Amend, true)]
    [InlineData(null, ImportNotificationType.Cveda, ImportNotificationStatus.InProgress, true)]
    [InlineData(null, ImportNotificationType.Cveda, ImportNotificationStatus.Modify, true)]
    [InlineData(null, ImportNotificationType.Cveda, ImportNotificationStatus.PartiallyRejected, true)]
    [InlineData(null, ImportNotificationType.Cveda, ImportNotificationStatus.Rejected, true)]
    [InlineData(null, ImportNotificationType.Cveda, ImportNotificationStatus.SplitConsignment, true)]
    [InlineData(null, ImportNotificationType.Cveda, ImportNotificationStatus.Validated, true)]
    [InlineData(null, ImportNotificationType.Ced, ImportNotificationStatus.Submitted, false)]
    [InlineData(null, ImportNotificationType.Cvedp, ImportNotificationStatus.Submitted, false)]
    [InlineData(null, ImportNotificationType.Chedpp, ImportNotificationStatus.Submitted, false)]
    [InlineData(false, ImportNotificationType.Cveda, ImportNotificationStatus.Submitted, true)]
    [InlineData(true, ImportNotificationType.Cveda, ImportNotificationStatus.Submitted, false)]
    public void CanFindDecisionTest(
        bool? iuuCheckRequired,
        string? importNotificationType,
        string notificationStatus,
        bool expectedResult
    )
    {
        var notification = new DecisionImportPreNotification
        {
            Id = "TEst",
            Status = notificationStatus,
            ImportNotificationType = importNotificationType,
            IuuCheckRequired = iuuCheckRequired,
        };
        var sut = new ChedADecisionFinder();

        var result = sut.CanFindDecision(notification, null, null);

        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(ConsignmentDecision.AcceptableForTranshipment, null, new[] { "Other" }, DecisionCode.E03)]
    [InlineData(ConsignmentDecision.AcceptableForTransit, null, new[] { "Other" }, DecisionCode.E03)]
    [InlineData(ConsignmentDecision.AcceptableForInternalMarket, null, new[] { "Other" }, DecisionCode.C03)]
    [InlineData(ConsignmentDecision.AcceptableForTemporaryImport, null, new[] { "Other" }, DecisionCode.C05)]
    [InlineData(ConsignmentDecision.HorseReEntry, null, new[] { "Other" }, DecisionCode.C06)]
    [InlineData(ConsignmentDecision.NonAcceptable, null, new[] { "Other" }, DecisionCode.N04)]
    [InlineData(
        ConsignmentDecision.AcceptableIfChanneled,
        null,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E96
    )]
    [InlineData(
        ConsignmentDecision.AcceptableForSpecificWarehouse,
        null,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E96
    )]
    [InlineData(
        ConsignmentDecision.AcceptableForPrivateImport,
        null,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E96
    )]
    [InlineData(
        ConsignmentDecision.AcceptableForTransfer,
        null,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E96
    )]
    [InlineData(null, null, null, DecisionCode.X00, DecisionInternalFurtherDetail.E99)]
    [InlineData(null, DecisionNotAcceptableAction.Euthanasia, null, DecisionCode.N02)]
    [InlineData(null, DecisionNotAcceptableAction.Reexport, new[] { "Other" }, DecisionCode.N04)]
    [InlineData(null, null, new[] { "Other" }, DecisionCode.N04)]
    [InlineData(null, null, new[] { "IdMismatchWithDocuments", "Other" }, DecisionCode.N04)]
    [InlineData(null, DecisionNotAcceptableAction.Slaughter, null, DecisionCode.N02)]
    [InlineData(
        null,
        DecisionNotAcceptableAction.Redispatching,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E97
    )]
    [InlineData(
        null,
        DecisionNotAcceptableAction.Destruction,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E97
    )]
    [InlineData(
        null,
        DecisionNotAcceptableAction.Transformation,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E97
    )]
    [InlineData(
        null,
        DecisionNotAcceptableAction.Other,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E97
    )]
    [InlineData(
        null,
        DecisionNotAcceptableAction.EntryRefusal,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E97
    )]
    [InlineData(
        null,
        DecisionNotAcceptableAction.QuarantineImposed,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E97
    )]
    [InlineData(
        null,
        DecisionNotAcceptableAction.SpecialTreatment,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E97
    )]
    [InlineData(
        null,
        DecisionNotAcceptableAction.IndustrialProcessing,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E97
    )]
    [InlineData(
        null,
        DecisionNotAcceptableAction.ReDispatch,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E97
    )]
    [InlineData(
        null,
        DecisionNotAcceptableAction.UseForOtherPurposes,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E97
    )]
    public void DecisionFinderTest(
        string? decision,
        string? notAcceptableAction,
        String[]? notAcceptableReasons,
        DecisionCode expectedCode,
        DecisionInternalFurtherDetail? expectedFurtherDetail = null
    )
    {
        var notification = new DecisionImportPreNotification
        {
            Id = "TEst",
            ConsignmentDecision = decision,
            NotAcceptableAction = notAcceptableAction,
            NotAcceptableReasons = notAcceptableReasons,
        };
        var sut = new ChedADecisionFinder();

        var result = sut.FindDecision(notification, null);

        result.DecisionCode.Should().Be(expectedCode);
        result.InternalDecisionCode.Should().Be(expectedFurtherDetail);
        result.CheckCode.Should().BeNull();
    }

    [Fact]
    public void WhenInspectionNotRequired_DecisionShouldBeHold()
    {
        var notification = new DecisionImportPreNotification
        {
            Id = "TEst",
            Status = ImportNotificationStatus.InProgress,
            InspectionRequired = InspectionRequired.NotRequired,
        };
        var sut = new ChedADecisionFinder();

        var result = sut.FindDecision(notification, null);

        result.DecisionCode.Should().Be(DecisionCode.H01);
    }

    [Fact]
    public void WhenInspectionRequired_DecisionShouldBeHold()
    {
        var notification = new DecisionImportPreNotification
        {
            Id = "TEst",
            Status = ImportNotificationStatus.InProgress,
            InspectionRequired = InspectionRequired.Required,
            Commodities = [new DecisionCommodityComplement { HmiDecision = CommodityRiskResultHmiDecision.Required }],
        };
        var sut = new ChedADecisionFinder();

        var result = sut.FindDecision(notification, null);

        result.DecisionCode.Should().Be(DecisionCode.H02);
    }

    [Fact]
    public void WhenPartiallyRejected_DecisionShouldBeX00()
    {
        var notification = new DecisionImportPreNotification
        {
            Id = "TEst",
            Status = ImportNotificationStatus.PartiallyRejected,
            InspectionRequired = InspectionRequired.Required,
            Commodities = [new DecisionCommodityComplement { HmiDecision = CommodityRiskResultHmiDecision.Required }],
        };
        var sut = new ChedADecisionFinder();

        var result = sut.FindDecision(notification, null);

        result.DecisionCode.Should().Be(DecisionCode.X00);
        result.InternalDecisionCode.Should().Be(DecisionInternalFurtherDetail.E80);
    }
}
