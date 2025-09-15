using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.Finders;

public class DecisionFinderTests
{
    [Theory]
    [InlineData(ImportNotificationStatus.Cancelled, true)]
    [InlineData(ImportNotificationStatus.Replaced, true)]
    [InlineData(ImportNotificationStatus.Deleted, true)]
    [InlineData(ImportNotificationStatus.SplitConsignment, true)]
    [InlineData("IGNORED", false)]
    public async Task FindDecision(string status, bool hasPartTwo)
    {
        var subject = new FixtureFinder();

        var result = subject.FindDecision(
            new DecisionImportPreNotification
            {
                Id = "id",
                Status = status,
                HasPartTwo = hasPartTwo,
            },
            new CheckCode { Value = "value" }
        );

        await Verify(result).UseParameters(status, hasPartTwo);
    }

    [Fact]
    public void FindDecision_FindDecisionInternal_Throws()
    {
        var subject = new FixtureFinder();

        var act = () =>
            subject.FindDecision(
                new DecisionImportPreNotification
                {
                    Id = "id",
                    Status = "IGNORED",
                    HasPartTwo = true,
                },
                new CheckCode { Value = "value" }
            );

        act.Should().Throw<NotImplementedException>();
    }

    private class FixtureFinder : DecisionFinder
    {
        protected override DecisionFinderResult FindDecisionInternal(
            DecisionImportPreNotification notification,
            CheckCode? checkCode
        )
        {
            throw new NotImplementedException();
        }

        protected override string ChedType { get; } = null!;

        public override bool CanFindDecision(
            DecisionImportPreNotification notification,
            CheckCode? checkCode,
            string? documentCode
        )
        {
            throw new NotImplementedException();
        }
    }
}
