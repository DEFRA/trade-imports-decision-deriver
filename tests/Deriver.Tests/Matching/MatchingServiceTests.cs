using AutoFixture;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsDataApi.Domain.Ipaffs.Constants;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.TestFixtures;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Matching;

public class MatchingServiceTests
{
    [Fact]
    public async Task SimpleNoMatchTest()
    {
        // Arrange
        var clearanceRequestWrappers = GenerateSimpleClearanceRequestWrapper();
        foreach (var commodity in clearanceRequestWrappers.ClearanceRequest.Commodities!)
        {
            foreach (var document in commodity.Documents!)
            {
                document.DocumentReference = new ImportDocumentReference("gbchd2025.1234567");
                document.DocumentCode = "C640";
            }
        }
        var sut = new MatchingService();
        var context = new MatchingContext(new List<DecisionImportPreNotification>(), [clearanceRequestWrappers]);

        // Act
        var matchResult = await sut.Process(context, CancellationToken.None);

        // Assert
        matchResult
            .NoMatches.Count.Should()
            .Be(clearanceRequestWrappers.ClearanceRequest.Commodities.Sum(x => x.Documents!.Length));
    }

    [Fact]
    public async Task SimpleIgnoreMatchTest()
    {
        // Arrange
        var clearanceRequestWrapper = GenerateSimpleClearanceRequestWrapper();
        foreach (var commodity in clearanceRequestWrapper.ClearanceRequest.Commodities!)
        {
            foreach (var document in commodity.Documents!)
            {
                document.DocumentReference = new ImportDocumentReference("INVALID");
            }
        }

        var sut = new MatchingService();
        var context = new MatchingContext(new List<DecisionImportPreNotification>(), [clearanceRequestWrapper]);

        // Act
        var matchResult = await sut.Process(context, CancellationToken.None);

        // Assert
        matchResult.NoMatches.Count.Should().Be(0);
        matchResult.NoMatches.Count.Should().Be(0);
    }

    [Fact]
    public async Task SimpleMatchTest()
    {
        // Arrange
        var clearanceRequestWrapper = GenerateSimpleClearanceRequestWrapper();
        foreach (var commodity in clearanceRequestWrapper.ClearanceRequest.Commodities!)
        {
            foreach (var document in commodity.Documents!)
            {
                document.DocumentReference = new ImportDocumentReference("gbchd2025.1234567");
                document.DocumentCode = "C640";
            }
        }

        var notification = ImportPreNotificationFixtures.ImportPreNotificationFixture(
            "CHEDP.GB.2025.1234567",
            ImportNotificationStatus.InProgress
        );
        var sut = new MatchingService();
        var context = new MatchingContext([notification.ToDecisionImportPreNotification()], [clearanceRequestWrapper]);

        // Act
        var matchResult = await sut.Process(context, CancellationToken.None);

        // Assert
        matchResult.NoMatches.Count.Should().Be(0);
        matchResult
            .Matches.Count.Should()
            .Be(clearanceRequestWrapper.ClearanceRequest.Commodities.Sum(x => x.Documents!.Length));
    }

    private static ClearanceRequestWrapper GenerateSimpleClearanceRequestWrapper()
    {
        var fixture = new Fixture();
        fixture.Customize<DateOnly>(o => o.FromFactory((DateTime dt) => DateOnly.FromDateTime(dt)));

        return fixture.Build<ClearanceRequestWrapper>().With(i => i.MovementReferenceNumber, "Test123").Create();
    }
}
