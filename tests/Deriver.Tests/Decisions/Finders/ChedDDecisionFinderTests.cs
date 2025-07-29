using Defra.TradeImportsDataApi.Domain.Ipaffs.Constants;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.Finders;

public class ChedDDecisionFinderTests
{
    [Theory]
    [InlineData(null, ImportNotificationType.Ced, ImportNotificationStatus.Submitted, true)]
    [InlineData(null, ImportNotificationType.Ced, ImportNotificationStatus.Amend, true)]
    [InlineData(null, ImportNotificationType.Ced, ImportNotificationStatus.InProgress, true)]
    [InlineData(null, ImportNotificationType.Ced, ImportNotificationStatus.Modify, true)]
    [InlineData(null, ImportNotificationType.Ced, ImportNotificationStatus.PartiallyRejected, true)]
    [InlineData(null, ImportNotificationType.Ced, ImportNotificationStatus.Rejected, true)]
    [InlineData(null, ImportNotificationType.Ced, ImportNotificationStatus.SplitConsignment, true)]
    [InlineData(null, ImportNotificationType.Ced, ImportNotificationStatus.Validated, true)]
    [InlineData(null, ImportNotificationType.Cveda, ImportNotificationStatus.Submitted, false)]
    [InlineData(null, ImportNotificationType.Cvedp, ImportNotificationStatus.Submitted, false)]
    [InlineData(null, ImportNotificationType.Chedpp, ImportNotificationStatus.Submitted, false)]
    [InlineData(false, ImportNotificationType.Ced, ImportNotificationStatus.Submitted, true)]
    [InlineData(true, ImportNotificationType.Ced, ImportNotificationStatus.Submitted, false)]
    public void CanFindDecisionTest(
        bool? iuuCheckRequired,
        string? importNotificationType,
        string notificationStatus,
        bool expectedResult
    )
    {
        var notification = new DecisionImportPreNotification
        {
            Id = "Test",
            Status = notificationStatus,
            ImportNotificationType = importNotificationType,
            IuuCheckRequired = iuuCheckRequired,
        };
        var sut = new ChedDDecisionFinder();

        var result = sut.CanFindDecision(notification, new CheckCode { Value = "H223" });

        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(ConsignmentDecision.AcceptableForInternalMarket, null, new[] { "Other" }, DecisionCode.C03)]
    [InlineData(
        ConsignmentDecision.AcceptableForTranshipment,
        null,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E96
    )]
    [InlineData(
        ConsignmentDecision.AcceptableForTransit,
        null,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E96
    )]
    [InlineData(
        ConsignmentDecision.AcceptableForTemporaryImport,
        null,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E96
    )]
    [InlineData(
        ConsignmentDecision.HorseReEntry,
        null,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E96
    )]
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
    [InlineData(null, null, new[] { "Other" }, DecisionCode.N04)]
    [InlineData(null, null, new[] { "PhysicalHygieneFailure", "ChemicalContamination" }, DecisionCode.N04)]
    [InlineData(null, DecisionNotAcceptableAction.Redispatching, new[] { "Other" }, DecisionCode.N04)]
    [InlineData(null, DecisionNotAcceptableAction.Destruction, null, DecisionCode.N02)]
    [InlineData(null, DecisionNotAcceptableAction.Transformation, new[] { "Other" }, DecisionCode.N03)]
    [InlineData(null, DecisionNotAcceptableAction.Other, new[] { "Other" }, DecisionCode.N07)]
    [InlineData(
        null,
        DecisionNotAcceptableAction.Euthanasia,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E97
    )]
    [InlineData(
        null,
        DecisionNotAcceptableAction.Reexport,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E97
    )]
    [InlineData(
        null,
        DecisionNotAcceptableAction.Slaughter,
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
            Id = "Test",
            ConsignmentDecision = decision,
            NotAcceptableAction = notAcceptableAction,
            NotAcceptableReasons = notAcceptableReasons,
        };
        var sut = new ChedDDecisionFinder();

        var result = sut.FindDecision(notification, null);

        result.DecisionCode.Should().Be(expectedCode);
        result.InternalDecisionCode.Should().Be(expectedFurtherDetail);
        result.CheckCode.Should().BeNull();
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
        var sut = new ChedDDecisionFinder();

        var result = sut.FindDecision(notification, null);

        result.DecisionCode.Should().Be(DecisionCode.X00);
        result.InternalDecisionCode.Should().Be(DecisionInternalFurtherDetail.E74);
    }
}
