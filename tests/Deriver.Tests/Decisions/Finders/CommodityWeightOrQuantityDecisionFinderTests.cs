using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.Finders;

public class CommodityWeightOrQuantityDecisionFinderTests
{
    private static string GetFormattedMessageFromLoggerCall(ILogger logger, int callIndex = 0)
    {
        var calls = logger.ReceivedCalls().ToArray();
        calls.Length.Should().BeGreaterThan(callIndex, "expected at least one log call");

        var call = calls[callIndex];
        // ILogger.Log<TState>(LogLevel, EventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        var args = call.GetArguments();
        var state = args[2];
        var ex = args[3] as Exception;
        var formatter = args[4] as Func<object, Exception?, string>;
        return (formatter is null ? state!.ToString() : formatter!(state!, ex))!;
    }

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

    [Fact]
    public void FindDecision_WhenInnerReturnsNonReleaseOrHold_ReturnsInnerResult_AndDoesNotLog()
    {
        var inner = Substitute.For<IDecisionFinder>();
        inner
            .FindDecision(Arg.Any<DecisionImportPreNotification>(), Arg.Any<Commodity>(), Arg.Any<CheckCode?>())
            .Returns(new DecisionFinderResult(DecisionCode.X00, null));
        var logger = Substitute.For<ILogger<CommodityWeightOrQuantityDecisionFinder>>();
        var sut = new CommodityWeightOrQuantityDecisionFinder(inner, logger);

        var notification = new DecisionImportPreNotification
        {
            Id = "id",
            Commodities = Array.Empty<DecisionCommodityComplement>(),
        };
        var commodity = new Commodity { TaricCommodityCode = "100" };

        var res = sut.FindDecision(notification, commodity, null);

        res.DecisionCode.Should().Be(DecisionCode.X00);
        // No logging should have occurred
        logger.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public void FindDecision_WithNetMass_TotalWeightGreater_LogsWarning()
    {
        var inner = Substitute.For<IDecisionFinder>();
        inner
            .FindDecision(Arg.Any<DecisionImportPreNotification>(), Arg.Any<Commodity>(), Arg.Any<CheckCode?>())
            .Returns(new DecisionFinderResult(DecisionCode.C03, null));

        var logger = Substitute.For<ILogger<CommodityWeightOrQuantityDecisionFinder>>();
        var sut = new CommodityWeightOrQuantityDecisionFinder(inner, logger);

        var notification = new DecisionImportPreNotification
        {
            Id = "id",
            Commodities = new[]
            {
                new DecisionCommodityComplement { CommodityCode = "100", Weight = 10M },
                new DecisionCommodityComplement { CommodityCode = "100", Weight = 5M },
            },
        };

        var commodity = new Commodity { TaricCommodityCode = "10099", NetMass = 10M };

        var result = sut.FindDecision(notification, commodity, null);

        result.DecisionCode.Should().Be(DecisionCode.C03);

        // One warning expected because totalWeight (15) > NetMass (10)
        logger.ReceivedCalls().Count().Should().BeGreaterThanOrEqualTo(1);
        var msg = GetFormattedMessageFromLoggerCall(logger, 0);
        msg.Should().Contain("Level 3 would have resulted in an X00 as IPAFFS NetWeight");
    }

    [Fact]
    public void FindDecision_WithNetMass_TotalWeightLess_LogsInformation()
    {
        var inner = Substitute.For<IDecisionFinder>();
        inner
            .FindDecision(Arg.Any<DecisionImportPreNotification>(), Arg.Any<Commodity>(), Arg.Any<CheckCode?>())
            .Returns(new DecisionFinderResult(DecisionCode.C03, null));

        var logger = Substitute.For<ILogger<CommodityWeightOrQuantityDecisionFinder>>();
        var sut = new CommodityWeightOrQuantityDecisionFinder(inner, logger);

        var notification = new DecisionImportPreNotification
        {
            Id = "id",
            Commodities = new[]
            {
                new DecisionCommodityComplement { CommodityCode = "100", Weight = 2M },
                new DecisionCommodityComplement { CommodityCode = "100", Weight = 3M },
            },
        };

        var commodity = new Commodity { TaricCommodityCode = "10099", NetMass = 10M };

        var result = sut.FindDecision(notification, commodity, null);

        result.DecisionCode.Should().Be(DecisionCode.C03);

        logger.ReceivedCalls().Count().Should().BeGreaterThanOrEqualTo(1);
        var msg = GetFormattedMessageFromLoggerCall(logger, 0);
        msg.Should().Contain("Level 3 would have succeeded as IPAFFS NetWeight");
    }

    [Fact]
    public void FindDecision_WithSupplementaryUnits_TotalQuantityGreater_LogsWarning()
    {
        var inner = Substitute.For<IDecisionFinder>();
        inner
            .FindDecision(Arg.Any<DecisionImportPreNotification>(), Arg.Any<Commodity>(), Arg.Any<CheckCode?>())
            .Returns(new DecisionFinderResult(DecisionCode.C03, null));

        var logger = Substitute.For<ILogger<CommodityWeightOrQuantityDecisionFinder>>();
        var sut = new CommodityWeightOrQuantityDecisionFinder(inner, logger);

        var notification = new DecisionImportPreNotification
        {
            Id = "id",
            Commodities = new[]
            {
                new DecisionCommodityComplement { CommodityCode = "200", Quantity = 10 },
                new DecisionCommodityComplement { CommodityCode = "200", Quantity = 5 },
            },
        };

        // Note: CompareQuantity uses commodity.NetMass in comparison in current implementation
        var commodity = new Commodity { TaricCommodityCode = "20099", SupplementaryUnits = 1 };

        var result = sut.FindDecision(notification, commodity, null);

        result.DecisionCode.Should().Be(DecisionCode.C03);

        logger.ReceivedCalls().Count().Should().BeGreaterThanOrEqualTo(1);
        var msg = GetFormattedMessageFromLoggerCall(logger, 0);
        msg.Should().Contain("Level 3 would have resulted in an X00 as IPAFFS NetQuantity");
    }

    [Fact]
    public void FindDecision_WithSupplementaryUnits_TotalQuantityLess_LogsInformation()
    {
        var inner = Substitute.For<IDecisionFinder>();
        inner
            .FindDecision(Arg.Any<DecisionImportPreNotification>(), Arg.Any<Commodity>(), Arg.Any<CheckCode?>())
            .Returns(new DecisionFinderResult(DecisionCode.C03, null));

        var logger = Substitute.For<ILogger<CommodityWeightOrQuantityDecisionFinder>>();
        var sut = new CommodityWeightOrQuantityDecisionFinder(inner, logger);

        var notification = new DecisionImportPreNotification
        {
            Id = "id",
            Commodities = new[]
            {
                new DecisionCommodityComplement { CommodityCode = "200", Quantity = 2 },
                new DecisionCommodityComplement { CommodityCode = "200", Quantity = 3 },
            },
        };

        var commodity = new Commodity { TaricCommodityCode = "20099", SupplementaryUnits = 6 };

        var result = sut.FindDecision(notification, commodity, null);

        result.DecisionCode.Should().Be(DecisionCode.C03);

        logger.ReceivedCalls().Count().Should().BeGreaterThanOrEqualTo(1);
        var msg = GetFormattedMessageFromLoggerCall(logger, 0);
        msg.Should().Contain("Level 3 would have succeeded as IPAFFS NetQuantity");
    }
}
