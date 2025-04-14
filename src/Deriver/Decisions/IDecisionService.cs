namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

public interface IDecisionService
{
    public Task<DecisionResult> Process(DecisionContext decisionContext, CancellationToken cancellationToken);
}
