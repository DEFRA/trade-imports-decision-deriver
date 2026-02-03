using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.Finders;

public class CommodityWeightOrQuantityDecisionFinderTests
{
    [Fact]
    public void ChedType_DelegatesToInnerFinder()
    {
        var inner = Substitute.For<IDecisionFinder>();
        inner.ChedType.Returns("CHEDX");
        var logger = Substitute.For<ILogger<CommodityWeightOrQuantityDecisionFinder>>();
        var sut = new CommodityWeightOrQuantityDecisionFinder(inner, logger);

        sut.ChedType.Should().Be("CHEDX");
    }

    [Fact]
    public void CanFindDecision_DelegatesToInnerFinder()
    {
        var inner = Substitute.For<IDecisionFinder>();
        var notif = new DecisionImportPreNotification
        {
            Id = "id",
            Commodities = Array.Empty<DecisionCommodityComplement>(),
        };
        inner.CanFindDecision(notif, Arg.Any<CheckCode?>(), Arg.Any<string?>()).Returns(true);

        var logger = Substitute.For<ILogger<CommodityWeightOrQuantityDecisionFinder>>();
        var sut = new CommodityWeightOrQuantityDecisionFinder(inner, logger);

        var result = sut.CanFindDecision(notif, null, null);

        result.Should().BeTrue();
        inner.Received(1).CanFindDecision(notif, null, null);
    }
}
