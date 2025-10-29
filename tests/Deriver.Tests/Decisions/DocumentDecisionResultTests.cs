using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions;

public class DocumentDecisionResultTests
{
    [Theory]
    [InlineData(
        "H220",
        DecisionInternalFurtherDetail.E70,
        "CHED reference test-ref cannot be found in IPAFFS. Check that the reference is correct."
    )]
    [InlineData("H224", DecisionInternalFurtherDetail.E70, null)]
    [InlineData("H224", DecisionInternalFurtherDetail.E71, DocumentDecisionReasons.CancelledChed)]
    [InlineData("H224", DecisionInternalFurtherDetail.E72, DocumentDecisionReasons.ReplacedChed)]
    [InlineData("H224", DecisionInternalFurtherDetail.E73, DocumentDecisionReasons.DeletedChed)]
    [InlineData("H220", DecisionInternalFurtherDetail.E74, DocumentDecisionReasons.SplitChed)]
    [InlineData("H220", DecisionInternalFurtherDetail.E75, DocumentDecisionReasons.UpdateCrToReferenceSplitChed)]
    [InlineData("H220", DecisionInternalFurtherDetail.E82, DocumentDecisionReasons.GmsInspectionAmend)]
    [InlineData("H224", DecisionInternalFurtherDetail.E84, DocumentDecisionReasons.CreateNewIpaffsNotification)]
    [InlineData("H219", DecisionInternalFurtherDetail.E85, DocumentDecisionReasons.PhsiCheckRequired)]
    [InlineData("H224", DecisionInternalFurtherDetail.E85, null)]
    [InlineData("H220", DecisionInternalFurtherDetail.E86, DocumentDecisionReasons.HmiCheckRequired)]
    [InlineData("H224", DecisionInternalFurtherDetail.E86, null)]
    [InlineData("H220", DecisionInternalFurtherDetail.E87, DocumentDecisionReasons.GmsInspection)]
    [InlineData("H224", DecisionInternalFurtherDetail.E93, null)]
    [InlineData("H224", DecisionInternalFurtherDetail.E94, null)]
    [InlineData("H224", DecisionInternalFurtherDetail.E99, DocumentDecisionReasons.UnknownError)]
    public void DocumentReasonTests(
        string checkCode,
        DecisionInternalFurtherDetail internalCode,
        string? expectedReason
    )
    {
        var sut = new DocumentDecisionResult(
            null,
            string.Empty,
            1,
            "test-ref",
            "test-code",
            checkCode,
            DecisionCode.X00,
            internalCode
        );

        sut.DecisionReason.Should().Be(expectedReason);
    }
}
