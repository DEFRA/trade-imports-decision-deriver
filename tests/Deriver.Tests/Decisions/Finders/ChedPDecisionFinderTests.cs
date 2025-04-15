using Defra.TradeImportsDataApi.Domain.Ipaffs;
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
        ImportNotificationType? importNotificationType,
        ImportNotificationStatus notificationStatus,
        bool expectedResult,
        string? checkCode
    )
    {
        var notification = new ImportPreNotification
        {
            Status = notificationStatus,
            ImportNotificationType = importNotificationType,
        };
        var sut = new ChedPDecisionFinder();

        var result = sut.CanFindDecision(
            notification,
            string.IsNullOrEmpty(checkCode) ? null : new CheckCode() { Value = checkCode }
        );

        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(true, ConsignmentDecision.AcceptableForInternalMarket, null, new[] { "Other" }, DecisionCode.C03)]
    [InlineData(true, ConsignmentDecision.AcceptableForTransit, null, new[] { "Other" }, DecisionCode.E03)]
    [InlineData(true, ConsignmentDecision.AcceptableIfChanneled, null, new[] { "Other" }, DecisionCode.C06)]
    [InlineData(true, ConsignmentDecision.AcceptableForTranshipment, null, new[] { "Other" }, DecisionCode.E03)]
    [InlineData(true, ConsignmentDecision.AcceptableForSpecificWarehouse, null, new[] { "Other" }, DecisionCode.E03)]
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
    [InlineData(false, null, DecisionNotAcceptableAction.Reexport, new[] { "Other" }, DecisionCode.N04)]
    [InlineData(false, null, null, new[] { "Other" }, DecisionCode.N04)]
    [InlineData(false, null, null, new[] { "InvasiveAlienSpecies", "NonApprovedCountry" }, DecisionCode.N04)]
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
        DecisionNotAcceptableAction.Slaughter,
        new[] { "Other" },
        DecisionCode.X00,
        DecisionInternalFurtherDetail.E97
    )]
    [InlineData(
        false,
        null,
        DecisionNotAcceptableAction.Redispatching,
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
        ConsignmentDecision? decision,
        DecisionNotAcceptableAction? notAcceptableAction,
        String[]? notAcceptableReasons,
        DecisionCode expectedCode,
        DecisionInternalFurtherDetail? expectedFurtherDetail = null
    )
    {
        var notification = new ImportPreNotification
        {
            PartTwo = new PartTwo
            {
                Decision = new Decision
                {
                    ConsignmentAcceptable = consignmentAcceptable,
                    ConsignmentDecision = decision,
                    NotAcceptableAction = notAcceptableAction,
                    NotAcceptableReasons = notAcceptableReasons,
                },
            },
        };
        var sut = new ChedPDecisionFinder();

        var result = sut.FindDecision(notification, null);

        result.DecisionCode.Should().Be(expectedCode);
        result.InternalDecisionCode.Should().Be(expectedFurtherDetail);
        result.CheckCode.Should().BeNull();
    }
}
