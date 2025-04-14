namespace Defra.TradeImportsDecisionDeriver.Deriver.Matching;

public interface IMatchingService
{
    public Task<MatchingResult> Process(MatchingContext matchingContext, CancellationToken cancellationToken);
}
