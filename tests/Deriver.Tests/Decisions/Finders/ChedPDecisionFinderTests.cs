using Defra.TradeImportsDataApi.Domain.Ipaffs.Constants;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.Finders;

public class ChedPDecisionFinderTests
{
    [Theory]
    [InlineData(ImportNotificationType.Cvedp, ImportNotificationStatus.Submitted, true, "H222")]
    [InlineData(ImportNotificationType.Cvedp, ImportNotificationStatus.Amend, true, "H222")]
    [InlineData(ImportNotificationType.Cvedp, ImportNotificationStatus.InProgress, true, "H222")]
    [InlineData(ImportNotificationType.Cvedp, ImportNotificationStatus.Modify, true, "H222")]
    [InlineData(ImportNotificationType.Cvedp, ImportNotificationStatus.PartiallyRejected, true, "H222")]
    [InlineData(ImportNotificationType.Cvedp, ImportNotificationStatus.Rejected, true, "H222")]
    [InlineData(ImportNotificationType.Cvedp, ImportNotificationStatus.SplitConsignment, true, "H222")]
    [InlineData(ImportNotificationType.Cvedp, ImportNotificationStatus.Validated, true, "H222")]
    [InlineData(ImportNotificationType.Cveda, ImportNotificationStatus.Submitted, false, "H222")]
    [InlineData(ImportNotificationType.Ced, ImportNotificationStatus.Submitted, false, "H222")]
    [InlineData(ImportNotificationType.Chedpp, ImportNotificationStatus.Submitted, false, "H222")]
    [InlineData(ImportNotificationType.Cvedp, ImportNotificationStatus.Submitted, true, null)]
    [InlineData(ImportNotificationType.Cvedp, ImportNotificationStatus.Submitted, false, "H224")]
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
        var sut = new ChedPDecisionFinder();

        var result = sut.CanFindDecision(
            notification,
            string.IsNullOrEmpty(checkCode) ? null : new CheckCode { Value = checkCode },
            null
        );

        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(ConsignmentDecision.AcceptableForInternalMarket, null, new[] { "Other" }, DecisionCode.C03)]
    [InlineData(ConsignmentDecision.AcceptableForTransit, null, new[] { "Other" }, DecisionCode.E03)]
    [InlineData(ConsignmentDecision.AcceptableIfChanneled, null, new[] { "Other" }, DecisionCode.C06)]
    [InlineData(ConsignmentDecision.AcceptableForTranshipment, null, new[] { "Other" }, DecisionCode.E03)]
    [InlineData(ConsignmentDecision.AcceptableForSpecificWarehouse, null, new[] { "Other" }, DecisionCode.E03)]
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
    [InlineData(null, DecisionNotAcceptableAction.Reexport, new[] { "Other" }, DecisionCode.N04)]
    [InlineData(null, null, new[] { "Other" }, DecisionCode.N04)]
    [InlineData(null, null, new[] { "InvasiveAlienSpecies", "NonApprovedCountry" }, DecisionCode.N04)]
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
        DecisionNotAcceptableAction.Slaughter,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E97
    )]
    [InlineData(
        null,
        DecisionNotAcceptableAction.Redispatching,
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
        var sut = new ChedPDecisionFinder();

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
        var sut = new ChedPDecisionFinder();

        var result = sut.FindDecision(notification, null);

        result.DecisionCode.Should().Be(DecisionCode.X00);
        result.InternalDecisionCode.Should().Be(DecisionInternalFurtherDetail.E80);
    }
}
