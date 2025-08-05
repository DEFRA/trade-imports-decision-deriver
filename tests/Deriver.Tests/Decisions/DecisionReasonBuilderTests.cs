using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions;

public class DecisionReasonBuilderTests
{
    // manual set
    [Fact]
    public void WhenDecisionReasonIsSetOnDecisionResult_ThenReasonShouldBeDecisionReason()
    {
        // Act
        var result = DecisionReasonBuilder.Build(
            new Commodity(),
            new DocumentDecisionResult(null, "Test", 1, string.Empty, null, null, DecisionCode.X00, "Test Reason", null)
        );

        // Assert
        result.Count.Should().Be(1);
        result[0].Should().Be("Test Reason");
    }

    [Fact]
    public void WhenDecisionResultIsNotLinkedToCheck_AndHasDocuments_ThenShouldBeNotLinkedReason()
    {
        // Act
        var result = DecisionReasonBuilder.Build(
            new Commodity()
            {
                Documents =
                [
                    new ImportDocument()
                    {
                        DocumentReference = new ImportDocumentReference("Test1234567"),
                        DocumentCode = "C640",
                    },
                ],
                Checks = [new CommodityCheck()],
            },
            new DocumentDecisionResult(
                null,
                "Test",
                1,
                "Test Doc Ref",
                "Test Doc Code",
                "H221",
                DecisionCode.X00,
                null,
                null
            )
        );

        // Assert
        result.Count.Should().Be(1);
        result[0]
            .Should()
            .Be(
                "A Customs Declaration has been submitted however no matching CVEDA(s) have been submitted to Port Health (for CVEDA number(s) Test1234567). Please correct the CVEDA number(s) entered on your customs declaration."
            );
    }

    [Fact]
    public void WhenDecisionResultIsNotLinkedToChed_AndHasNoDocuments_AndCheckCodeIsH220_ThenShouldBeHmiGmsReason()
    {
        // Act
        var result = DecisionReasonBuilder.Build(
            new Commodity() { Checks = [new CommodityCheck() { CheckCode = "H220" }] },
            new DocumentDecisionResult(
                null,
                "Test",
                1,
                "Test Doc Ref",
                "Test Doc Code",
                "H221",
                DecisionCode.X00,
                null,
                null
            )
        );

        // Assert
        result.Count.Should().Be(1);
        result[0]
            .Should()
            .Be(
                "A Customs Declaration with a GMS product has been selected for HMI inspection. In IPAFFS create a CHEDPP and amend your licence to reference it. If a CHEDPP exists, amend your licence to reference it. Failure to do so will delay your Customs release."
            );
    }

    [Fact]
    public void WhenDecisionResultIsNotLinkedToChed_AndHasNotDocuments_AndCheckCodeIsNotH220_ThenShouldBeNoReason()
    {
        // Act
        var result = DecisionReasonBuilder.Build(
            new Commodity() { Checks = [new CommodityCheck() { CheckCode = "H221" }] },
            new DocumentDecisionResult(
                null,
                "Test",
                1,
                "Test Doc Ref",
                "Test Doc Code",
                "H221",
                DecisionCode.X00,
                null,
                null
            )
        );

        // Assert
        result.Count.Should().Be(0);
    }
}
