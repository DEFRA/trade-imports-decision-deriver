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
    [InlineData(true, ConsignmentDecision.AcceptableForInternalMarket, null, new[] { "Other" }, DecisionCode.C03)]
    [InlineData(
        true,
        ConsignmentDecision.AcceptableForTranshipment,
        null,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E96
    )]
    [InlineData(
        true,
        ConsignmentDecision.AcceptableForTransit,
        null,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E96
    )]
    [InlineData(
        true,
        ConsignmentDecision.AcceptableForTemporaryImport,
        null,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E96
    )]
    [InlineData(
        true,
        ConsignmentDecision.HorseReEntry,
        null,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E96
    )]
    [InlineData(
        true,
        ConsignmentDecision.NonAcceptable,
        null,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E96
    )]
    [InlineData(
        true,
        ConsignmentDecision.AcceptableIfChanneled,
        null,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E96
    )]
    [InlineData(
        true,
        ConsignmentDecision.AcceptableForSpecificWarehouse,
        null,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E96
    )]
    [InlineData(
        true,
        ConsignmentDecision.AcceptableForPrivateImport,
        null,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E96
    )]
    [InlineData(
        true,
        ConsignmentDecision.AcceptableForTransfer,
        null,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E96
    )]
    [InlineData(null, null, null, new[] { "Other" }, DecisionCode.X00, DecisionInternalFurtherDetail.E99)]
    [InlineData(false, null, null, new[] { "Other" }, DecisionCode.N04)]
    [InlineData(false, null, null, new[] { "PhysicalHygieneFailure", "ChemicalContamination" }, DecisionCode.N04)]
    [InlineData(false, null, DecisionNotAcceptableAction.Redispatching, new[] { "Other" }, DecisionCode.N04)]
    [InlineData(false, null, DecisionNotAcceptableAction.Destruction, null, DecisionCode.N02)]
    [InlineData(false, null, DecisionNotAcceptableAction.Transformation, new[] { "Other" }, DecisionCode.N03)]
    [InlineData(false, null, DecisionNotAcceptableAction.Other, new[] { "Other" }, DecisionCode.N07)]
    [InlineData(
        false,
        null,
        DecisionNotAcceptableAction.Euthanasia,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E97
    )]
    [InlineData(
        false,
        null,
        DecisionNotAcceptableAction.Reexport,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E97
    )]
    [InlineData(
        false,
        null,
        DecisionNotAcceptableAction.Slaughter,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E97
    )]
    [InlineData(
        false,
        null,
        DecisionNotAcceptableAction.EntryRefusal,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E97
    )]
    [InlineData(
        false,
        null,
        DecisionNotAcceptableAction.QuarantineImposed,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E97
    )]
    [InlineData(
        false,
        null,
        DecisionNotAcceptableAction.SpecialTreatment,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E97
    )]
    [InlineData(
        false,
        null,
        DecisionNotAcceptableAction.IndustrialProcessing,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E97
    )]
    [InlineData(
        false,
        null,
        DecisionNotAcceptableAction.ReDispatch,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E97
    )]
    [InlineData(
        false,
        null,
        DecisionNotAcceptableAction.UseForOtherPurposes,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E97
    )]
    public void DecisionFinderTest(
        bool? consignmentAcceptable,
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
            ConsignmentAcceptable = consignmentAcceptable,
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
}
