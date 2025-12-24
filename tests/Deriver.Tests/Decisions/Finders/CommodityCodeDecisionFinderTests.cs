using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.Finders
{
    public class CommodityCodeDecisionFinderTests
    {
        [Fact]
        public void ChedType_ReturnsInnerChedType()
        {
            // Arrange
            var innerMock = Substitute.For<IDecisionFinder>();
            innerMock.ChedType.Returns("TEST-Ched");
            var loggerMock = Substitute.For<ILogger<CommodityCodeDecisionFinder>>();
            var finder = new CommodityCodeDecisionFinder(innerMock, loggerMock);

            // Act
            var chedType = finder.ChedType;

            // Assert
            Assert.Equal("TEST-Ched", chedType);
        }

        [Fact]
        public void CanFindDecision_DelegatesToInnerDecisionFinder()
        {
            // Arrange
            var innerMock = Substitute.For<IDecisionFinder>();
            var notification = new DecisionImportPreNotification { Id = "notif-1" };
            CheckCode? checkCode = null;
            var docCode = "DOC";
            innerMock.CanFindDecision(notification, checkCode, docCode).Returns(true);

            var loggerMock = Substitute.For<ILogger<CommodityCodeDecisionFinder>>();
            var finder = new CommodityCodeDecisionFinder(innerMock, loggerMock);

            // Act
            var result = finder.CanFindDecision(notification, checkCode, docCode);

            // Assert
            Assert.True(result);
            innerMock.Received(1).CanFindDecision(notification, checkCode, docCode);
        }

        [Fact]
        public void FindDecision_LogsWarning_When_NoMatchingCommodityAndResultIsReleaseOrHold()
        {
            // Arrange
            var innerMock = Substitute.For<IDecisionFinder>();
            var loggerMock = Substitute.For<ILogger<CommodityCodeDecisionFinder>>();

            // Create a DecisionFinderResult with a DecisionCode likely considered ReleaseOrHold.
            var expectedResult = new DecisionFinderResult(DecisionCode.C02, new CheckCode() { Value = "H219" });

            innerMock
                .FindDecision(Arg.Any<DecisionImportPreNotification>(), Arg.Any<Commodity>(), Arg.Any<CheckCode?>())
                .Returns(expectedResult);

            var finder = new CommodityCodeDecisionFinder(innerMock, loggerMock);

            var notification = new DecisionImportPreNotification
            {
                Id = "n1",
                Commodities = new[]
                {
                    new DecisionCommodityComplement { CommodityCode = "12" }, // does not match "999"
                },
            };

            var commodity = new Commodity { TaricCommodityCode = "999999", ItemNumber = 1 };

            // Act
            var result = finder.FindDecision(notification, commodity, null);

            // Assert: returned the same result instance
            Assert.Same(expectedResult, result);

            // Verify a LogWarning was written containing hint text.
            loggerMock
                .Received(1)
                .Log(
                    LogLevel.Warning,
                    Arg.Any<EventId>(),
                    Arg.Is<object>(v => v.ToString()!.Contains("Level 2 would have resulted")),
                    Arg.Any<Exception>(),
                    Arg.Any<Func<object, Exception, string>>()!
                );
        }

        [Fact]
        public void FindDecision_DoesNotLogWarning_When_MatchingCommodityExists()
        {
            // Arrange
            var innerMock = Substitute.For<IDecisionFinder>();
            var loggerMock = Substitute.For<ILogger<CommodityCodeDecisionFinder>>();

            var expectedResult = new DecisionFinderResult(DecisionCode.C02, new CheckCode() { Value = "H219" });

            innerMock
                .FindDecision(Arg.Any<DecisionImportPreNotification>(), Arg.Any<Commodity>(), Arg.Any<CheckCode?>())
                .Returns(expectedResult);

            var finder = new CommodityCodeDecisionFinder(innerMock, loggerMock);

            var notification = new DecisionImportPreNotification
            {
                Id = "n2",
                Commodities = new[]
                {
                    new DecisionCommodityComplement { CommodityCode = "123" }, // will match prefix of "12345"
                },
            };

            var commodity = new Commodity { TaricCommodityCode = "12345", ItemNumber = 2 };

            // Act
            var result = finder.FindDecision(notification, commodity, null);

            // Assert
            Assert.Same(expectedResult, result);

            // Verify no warning message was logged.
            loggerMock
                .DidNotReceive()
                .Log(
                    LogLevel.Warning,
                    Arg.Any<EventId>(),
                    Arg.Any<object>(),
                    Arg.Any<Exception>(),
                    Arg.Any<Func<object, Exception, string>>()!
                );
        }
    }
}
