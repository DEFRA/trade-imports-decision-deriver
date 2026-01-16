using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Processors;

public interface IDecisionService
{
    IReadOnlyList<(string Mrn, ClearanceDecision Decision)> Process(DecisionContext context);
}

public class DecisionService(IClearanceDecisionBuilder clearanceDecisionBuilder, ICheckProcessor checkProcessor)
    : IDecisionService
{
    public IReadOnlyList<(string Mrn, ClearanceDecision Decision)> Process(DecisionContext context)
    {
        return context.CustomsDeclarations.Select(wrapper => Process(context, wrapper)).ToList();
    }

    private (string Mrn, ClearanceDecision Decision) Process(
        DecisionContext context,
        CustomsDeclarationWrapper customsDeclaration
    )
    {
        var commodities = customsDeclaration.CustomsDeclaration.ClearanceRequest?.Commodities ?? [];
        List<CheckDecisionResult> checkDecisionResults = [];
        foreach (var commodity in commodities)
        {
            checkDecisionResults.AddRange(Process(context, customsDeclaration, commodity));
        }

        var newClearanceDecision = clearanceDecisionBuilder.BuildClearanceDecision(
            customsDeclaration.MovementReferenceNumber,
            checkDecisionResults,
            customsDeclaration.CustomsDeclaration
        );

        return (customsDeclaration.MovementReferenceNumber, newClearanceDecision);
    }

    public IEnumerable<CheckDecisionResult> Process(
        DecisionContext context,
        CustomsDeclarationWrapper clearanceRequest,
        Commodity commodity
    )
    {
        var checks = commodity.Checks?.DistinctBy(x => x.CheckCode).ToArray();
        if (checks == null || checks.Length == 0)
        {
            return Array.Empty<CheckDecisionResult>();
        }

        var output = new List<CheckDecisionResult>(checks.Length);

        for (var i = 0; i < checks.Length; i++)
        {
            var results = checkProcessor.Process(context, clearanceRequest, commodity, checks[i]);

            output.AddRange(results);
        }

        return output;
    }
}
