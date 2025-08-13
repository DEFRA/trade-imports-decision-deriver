using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions;

public class DecisionReasonBuilderTests
{
    [Fact]
    public void WhenDecisionResultIsNotLinkedToCheck_AndCheda_AndHasDocuments_ThenShouldBeNotLinkedReason()
    {
        DocumentDecisionResult[] documentDecisionResults =
        [
            new(null, "Test", 1, "Test1234567", "C640", "H221", DecisionCode.X00, null, null),
        ];
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
            documentDecisionResults[0],
            documentDecisionResults
        );

        // Assert
        result.Count.Should().Be(1);
        result[0]
            .Should()
            .Be(
                "A Customs Declaration has been submitted however no matching CVEDA(s) have been submitted to Animal Health (for CVEDA number(s) Test1234567). Please correct the CVEDA number(s) entered on your customs declaration."
            );
    }

    [Fact]
    public void WhenDecisionResultIsNotLinkedToCheck_AndNotCheda_AndHasDocuments_ThenShouldBeNotLinkedReason()
    {
        DocumentDecisionResult[] documentDecisionResults =
        [
            new(null, "Test", 1, "Test1234567", "9115", "H221", DecisionCode.X00, null, null),
        ];
        // Act
        var result = DecisionReasonBuilder.Build(
            new Commodity()
            {
                Documents =
                [
                    new ImportDocument()
                    {
                        DocumentReference = new ImportDocumentReference("Test1234567"),
                        DocumentCode = "9115",
                    },
                ],
                Checks = [new CommodityCheck()],
            },
            documentDecisionResults[0],
            documentDecisionResults
        );

        // Assert
        result.Count.Should().Be(1);
        result[0]
            .Should()
            .Be(
                "A Customs Declaration has been submitted however no matching CHEDPP(s) have been submitted to Port Health (for CHEDPP number(s) Test1234567). Please correct the CHEDPP number(s) entered on your customs declaration."
            );
    }

    [Fact]
    public void WhenDecisionResultIsNotLinkedToChed_AndHasNoDocuments_AndCheckCodeIsH220_ThenShouldBeHmiGmsReason()
    {
        DocumentDecisionResult[] documentDecisionResults =
        [
            new(null, "Test", 1, "Test Doc Ref", "Test Doc Code", "H220", DecisionCode.X00, null, null),
        ];

        // Act
        var result = DecisionReasonBuilder.Build(
            new Commodity()
            {
                Documents =
                [
                    new ImportDocument()
                    {
                        DocumentReference = new ImportDocumentReference("Test1234567"),
                        DocumentCode = "9115",
                    },
                ],
                Checks = [new CommodityCheck() { CheckCode = "H220" }],
            },
            documentDecisionResults[0],
            documentDecisionResults
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
        DocumentDecisionResult[] documentDecisionResults =
        [
            new(null, "Test", 1, "Test Doc Ref", "Test Doc Code", "H221", DecisionCode.X00, null, null),
        ];

        // Act
        var result = DecisionReasonBuilder.Build(
            new Commodity() { Checks = [new CommodityCheck() { CheckCode = "H221" }] },
            documentDecisionResults[0],
            documentDecisionResults
        );

        // Assert
        result.Count.Should().Be(0);
    }
}
