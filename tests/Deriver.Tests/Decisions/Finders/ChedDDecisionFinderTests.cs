using Defra.TradeImportsDataApi.Domain.Ipaffs.Constants;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.Finders;

public class ChedDDecisionFinderTests
{
    [Theory]
    [InlineData(null, ImportNotificationType.Ced, "H223", "C678", true)]
    [InlineData(null, ImportNotificationType.Ced, "H223", "N852", true)]
    [InlineData(null, ImportNotificationType.Ced, "H223", "C085", false)]
    [InlineData(null, ImportNotificationType.Ced, "H223", "N002", false)]
    [InlineData(null, ImportNotificationType.Ced, "H223", "N851", false)]
    [InlineData(null, ImportNotificationType.Ced, "H223", "9115", false)]
    [InlineData(null, ImportNotificationType.Ced, "H223", "C640", false)]
    [InlineData(null, ImportNotificationType.Ced, "H223", "N853", false)]
    [InlineData(null, ImportNotificationType.Ced, "H223", "C641", false)]
    [InlineData(null, ImportNotificationType.Ced, "H223", "C673", false)]
    [InlineData(null, ImportNotificationType.Ced, "H223", "C674", false)]
    [InlineData(null, ImportNotificationType.Cvedp, "H223", "C678", true)]
    [InlineData(null, ImportNotificationType.Chedpp, "H223", "C678", true)]
    [InlineData(null, ImportNotificationType.Cveda, "H223", "C678", true)]
    [InlineData(true, ImportNotificationType.Ced, "H223", "C678", false)]
    public void CanFindDecisionTest(
        bool? iuuCheckRequired,
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
            IuuCheckRequired = iuuCheckRequired,
        };
        var sut = new ChedDDecisionFinder();

        var result = sut.CanFindDecision(notification, new CheckCode { Value = checkCode }, documentCode);

        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(ConsignmentDecision.AcceptableForInternalMarket, null, new[] { "Other" }, DecisionCode.C03)]
    [InlineData(ConsignmentDecision.AcceptableForNonInternalMarket, null, new[] { "Other" }, DecisionCode.C03)]
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
            ImportNotificationType = ImportNotificationType.Ced,
            ConsignmentDecision = decision,
            NotAcceptableAction = notAcceptableAction,
            NotAcceptableReasons = notAcceptableReasons,
            HasPartTwo = true,
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
            ImportNotificationType = ImportNotificationType.Ced,
            Status = ImportNotificationStatus.PartiallyRejected,
            InspectionRequired = InspectionRequired.Required,
            Commodities = [new DecisionCommodityComplement { HmiDecision = CommodityRiskResultHmiDecision.Required }],
            HasPartTwo = true,
        };
        var sut = new ChedDDecisionFinder();

        var result = sut.FindDecision(notification, null);

        result.DecisionCode.Should().Be(DecisionCode.X00);
        result.InternalDecisionCode.Should().Be(DecisionInternalFurtherDetail.E74);
    }

    [Fact]
    public void WhenMissingPartTwo_DecisionShouldBeX00()
    {
        var notification = new DecisionImportPreNotification
        {
            Id = "TEst",
            ImportNotificationType = ImportNotificationType.Ced,
            Status = ImportNotificationStatus.Submitted,
        };
        var sut = new ChedDDecisionFinder();

        var result = sut.FindDecision(notification, null);

        result.DecisionCode.Should().Be(DecisionCode.X00);
        result.InternalDecisionCode.Should().Be(DecisionInternalFurtherDetail.E88);
    }
}
