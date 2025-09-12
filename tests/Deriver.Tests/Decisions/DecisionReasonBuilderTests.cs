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
            new ClearanceRequest(),
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
        result[0].Should().Be(DecisionReasonBuilder.AnimalHealthErrorMessage("CVEDA", "Test1234567"));
    }

    [Fact]
    public void WhenDecisionResultIsNotLinkedToCheck_AndIuu_AndHasDocuments_ThenShouldBeNotLinkedReason()
    {
        DocumentDecisionResult[] documentDecisionResults =
        [
            new(null, "Test", 1, "Test1234567", "C641", "H221", DecisionCode.X00, null, null),
        ];
        // Act
        var result = DecisionReasonBuilder.Build(
            new ClearanceRequest(),
            new Commodity()
            {
                Documents =
                [
                    new ImportDocument()
                    {
                        DocumentReference = new ImportDocumentReference("Test1234567"),
                        DocumentCode = "C641",
                    },
                ],
                Checks = [new CommodityCheck()],
            },
            documentDecisionResults[0],
            documentDecisionResults
        );

        // Assert
        result.Count.Should().Be(1);
        result[0].Should().Be(DecisionReasonBuilder.IuuErrorMessage);
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
            new ClearanceRequest(),
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
        result[0].Should().Be(DecisionReasonBuilder.PortHealthErrorMessage("CHEDPP", "Test1234567"));
    }

    [Fact]
    public void WhenDecisionResultIsNotLinkedToChed_AndHasInvalidDocuments_AndCheckCodeIsH220_ThenShouldBeHmiGmsReason()
    {
        DocumentDecisionResult[] documentDecisionResults =
        [
            new(null, "Test", 1, "Test Doc Ref", "Test Doc Code", "H220", DecisionCode.X00, null, null),
        ];

        // Act
        var cr = new ClearanceRequest() { DeclarationUcr = "TestUcr" };
        var item = new Commodity()
        {
            ItemNumber = 7,
            NetMass = (decimal?)42.3,
            TaricCommodityCode = "test-code",
            GoodsDescription = "test-description",
            Documents =
            [
                new ImportDocument()
                {
                    DocumentReference = new ImportDocumentReference("Test1234567"),
                    DocumentCode = "9115",
                },
            ],
            Checks = [new CommodityCheck() { CheckCode = "H220" }],
        };
        var result = DecisionReasonBuilder.Build(cr, item, documentDecisionResults[0], documentDecisionResults);

        // Assert
        result.Count.Should().Be(1);
        result[0]
            .Should()
            .Be(
                DecisionReasonBuilder.GmsErrorMessage(
                    cr.DeclarationUcr,
                    item.ItemNumber,
                    item.NetMass,
                    item.TaricCommodityCode,
                    item.GoodsDescription
                )
            );
    }

    [Fact]
    public void WhenDecisionResultIsNotLinkedToChed_AndHasNoDocuments_AndCheckCodeIsH220_ThenShouldBeHmiGmsReason()
    {
        DocumentDecisionResult[] documentDecisionResults =
        [
            new(null, "Test", 1, "", "", "H220", DecisionCode.X00, null, null),
        ];

        // Act
        var cr = new ClearanceRequest() { DeclarationUcr = "TestUcr" };
        var item = new Commodity()
        {
            ItemNumber = 7,
            NetMass = (decimal?)42.3,
            TaricCommodityCode = "test-code",
            GoodsDescription = "test-description",
            Documents = [],
            Checks = [new CommodityCheck() { CheckCode = "H220" }],
        };
        var result = DecisionReasonBuilder.Build(cr, item, documentDecisionResults[0], documentDecisionResults);

        // Assert
        result.Count.Should().Be(1);
        result[0]
            .Should()
            .Be(
                DecisionReasonBuilder.GmsErrorMessage(
                    cr.DeclarationUcr,
                    item.ItemNumber,
                    item.NetMass,
                    item.TaricCommodityCode,
                    item.GoodsDescription
                )
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
            new ClearanceRequest(),
            new Commodity() { Checks = [new CommodityCheck() { CheckCode = "H221" }] },
            documentDecisionResults[0],
            documentDecisionResults
        );

        // Assert
        result.Count.Should().Be(0);
    }

    [Fact]
    public void When_DocumentCode_IsInvalid_ThenShouldThrow()
    {
        DocumentDecisionResult[] documentDecisionResults =
        [
            new(null, "Test", 1, "Test Doc Ref", "Test Doc Code", "H224", DecisionCode.X00, null, null),
        ];

        // Act
        Action act = () =>
            DecisionReasonBuilder.Build(
                new ClearanceRequest(),
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
                    Checks = [new CommodityCheck() { CheckCode = "H224" }],
                },
                documentDecisionResults[0],
                documentDecisionResults
            );

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
