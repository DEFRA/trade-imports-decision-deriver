using Defra.TradeImportsDataApi.Domain.Ipaffs.Constants;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.Finders;

public class IuuDecisionFinderTests
{
    [Theory]
    [InlineData(ImportNotificationType.Cvedp, "H222", "C085", false)]
    [InlineData(ImportNotificationType.Cvedp, "H222", "N002", false)]
    [InlineData(ImportNotificationType.Cvedp, "H222", "9115", false)]
    [InlineData(ImportNotificationType.Cvedp, "H222", "N853", false)]
    [InlineData(ImportNotificationType.Cvedp, "H222", "C640", false)]
    [InlineData(ImportNotificationType.Cvedp, "H222", "C678", false)]
    [InlineData(ImportNotificationType.Cvedp, "H222", "N852", false)]
    [InlineData(ImportNotificationType.Cvedp, "H222", "C641", false)]
    [InlineData(ImportNotificationType.Cvedp, "H222", "C673", false)]
    [InlineData(ImportNotificationType.Cvedp, "H222", "C674", false)]
    [InlineData(ImportNotificationType.Cvedp, "H224", "C085", false)]
    [InlineData(ImportNotificationType.Cvedp, "H224", "N002", false)]
    [InlineData(ImportNotificationType.Cvedp, "H224", "9115", false)]
    [InlineData(ImportNotificationType.Cvedp, "H224", "N853", true)]
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
        var sut = new IuuDecisionFinder();

        var result = sut.CanFindDecision(
            notification,
            string.IsNullOrEmpty(checkCode) ? null : new CheckCode { Value = checkCode },
            documentCode
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
            ImportNotificationType = ImportNotificationType.Cvedp,
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
