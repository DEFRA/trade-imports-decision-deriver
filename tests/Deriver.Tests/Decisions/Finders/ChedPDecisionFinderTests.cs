using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Ipaffs.Constants;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.Finders;

public class ChedPDecisionFinderTests
{
    [Theory]
    [InlineData(ImportNotificationType.Cvedp, "H222", "C085", false)]
    [InlineData(ImportNotificationType.Cvedp, "H222", "N002", false)]
    [InlineData(ImportNotificationType.Cvedp, "H222", "9115", false)]
    [InlineData(ImportNotificationType.Cvedp, "H222", "N853", true)]
    [InlineData(ImportNotificationType.Cvedp, "H222", "C640", false)]
    [InlineData(ImportNotificationType.Cvedp, "H222", "C678", false)]
    [InlineData(ImportNotificationType.Cvedp, "H222", "N852", false)]
    [InlineData(ImportNotificationType.Cvedp, "H222", "C641", false)]
    [InlineData(ImportNotificationType.Cvedp, "H222", "C673", false)]
    [InlineData(ImportNotificationType.Cvedp, "H222", "C674", false)]
    [InlineData(ImportNotificationType.Cvedp, "H224", "C085", false)]
    [InlineData(ImportNotificationType.Cvedp, "H224", "N002", false)]
    [InlineData(ImportNotificationType.Cvedp, "H224", "9115", false)]
    [InlineData(ImportNotificationType.Cvedp, "H224", "N853", false)]
    [InlineData(ImportNotificationType.Cvedp, "H224", "C640", false)]
    [InlineData(ImportNotificationType.Cvedp, "H224", "C678", false)]
    [InlineData(ImportNotificationType.Cvedp, "H224", "N852", false)]
    [InlineData(ImportNotificationType.Cvedp, "H224", "C641", false)]
    [InlineData(ImportNotificationType.Cvedp, "H224", "C673", false)]
    [InlineData(ImportNotificationType.Cvedp, "H224", "C674", false)]
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
        var sut = new ChedPDecisionFinder();

        var result = sut.CanFindDecision(
            notification,
            string.IsNullOrEmpty(checkCode) ? null : new CheckCode { Value = checkCode },
            documentCode
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
            ImportNotificationType = ImportNotificationType.Cvedp,
            ConsignmentDecision = decision,
            NotAcceptableAction = notAcceptableAction,
            NotAcceptableReasons = notAcceptableReasons,
            HasPartTwo = true,
        };
        var sut = new ChedPDecisionFinder();

        var result = sut.FindDecision(notification, new Commodity(), null);

        result.DecisionCode.Should().Be(expectedCode);
        result.InternalDecisionCode.Should().Be(expectedFurtherDetail);
        result.CheckCode.Should().BeNull();
    }

    [Theory]
    [InlineData(ImportNotificationStatus.Submitted)]
    [InlineData(ImportNotificationStatus.InProgress)]
    [InlineData(ImportNotificationStatus.Amend)]
    public void WhenInspectionNotRequired_DecisionShouldBeHold(string notificationStatus)
    {
        var notification = new DecisionImportPreNotification
        {
            Id = "TEst",
            ImportNotificationType = ImportNotificationType.Cvedp,
            Status = notificationStatus,
            InspectionRequired = InspectionRequired.NotRequired,
            HasPartTwo = true,
        };
        var sut = new ChedPDecisionFinder();

        var result = sut.FindDecision(notification, new Commodity(), new CheckCode() { Value = "H221" });

        result.DecisionCode.Should().Be(DecisionCode.H01);
    }

    [Theory]
    [InlineData(ImportNotificationStatus.Submitted)]
    [InlineData(ImportNotificationStatus.InProgress)]
    [InlineData(ImportNotificationStatus.Amend)]
    public void WhenInspectionRequired_DecisionShouldBeHold(string notificationStatus)
    {
        var notification = new DecisionImportPreNotification
        {
            Id = "TEst",
            ImportNotificationType = ImportNotificationType.Cvedp,
            Status = notificationStatus,
            InspectionRequired = InspectionRequired.Required,
            Commodities = [new DecisionCommodityComplement { HmiDecision = CommodityRiskResultHmiDecision.Required }],
            HasPartTwo = true,
        };
        var sut = new ChedPDecisionFinder();

        var result = sut.FindDecision(notification, new Commodity(), new CheckCode() { Value = "H221" });

        result.DecisionCode.Should().Be(DecisionCode.H02);
    }

    [Fact]
    public void WhenMissingPartTwo_DecisionShouldBeH01()
    {
        var notification = new DecisionImportPreNotification
        {
            Id = "TEst",
            ImportNotificationType = ImportNotificationType.Cvedp,
            Status = ImportNotificationStatus.Submitted,
        };
        var sut = new ChedPDecisionFinder();

        var result = sut.FindDecision(notification, new Commodity(), null);

        result.DecisionCode.Should().Be(DecisionCode.H01);
        result.InternalDecisionCode.Should().Be(DecisionInternalFurtherDetail.E88);
    }
}
