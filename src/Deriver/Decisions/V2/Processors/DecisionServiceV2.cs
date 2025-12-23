using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.Processors;

////public interface IDecisionContextProcessor
////{
////    IEnumerable<ClearanceDecision> Process(DecisionContextV2 context);
////}

public interface IDecisionServiceV2
{
    IReadOnlyList<(string Mrn, ClearanceDecision Decision)> Process(DecisionContextV2 context);
}

////public class DecisionContextProcessor(IClearanceRequestProcessor clearanceRequestProcessor) : IDecisionContextProcessor
////{
////    public IEnumerable<ClearanceDecision> Process(DecisionContextV2 context)
////    {
////        foreach (var wrapper in context.CustomsDeclarations)
////        {
////            yield return Process(context, wrapper);
////        }
////    }

////    private ClearanceDecision Process(DecisionContextV2 context, CustomsDeclarationWrapper customsDeclaration)
////    {
////        var commodities = customsDeclaration.CustomsDeclaration.ClearanceRequest?.Commodities ?? [];
////        List<CheckDecisionResult> checkDecisionResults = [];
////        foreach (var commodity in commodities)
////        {
////            checkDecisionResults.AddRange(Process(context, customsDeclaration, commodity));
////        }

////        var newClearanceDecision = clearanceDecisionBuilder.BuildClearanceDecision(
////            customsDeclaration.MovementReferenceNumber,
////            checkDecisionResults,
////            customsDeclaration.CustomsDeclaration
////        );

////        return newClearanceDecision;
////    }

////    public IEnumerable<CheckDecisionResult> Process(
////        DecisionContextV2 context,
////        CustomsDeclarationWrapper clearanceRequest,
////        Commodity commodity
////    )
////    {
////        var checks = commodity.Checks;
////        if (checks == null || checks.Length == 0)
////        {
////            return Array.Empty<CheckDecisionResult>();
////        }

////        var output = new List<CheckDecisionResult>(checks.Length);

////        for (var i = 0; i < checks.Length; i++)
////        {
////            var results = checkProcessor.Process(context, clearanceRequest, commodity, checks[i]);

////            output.AddRange(results);
////        }

////        return output;
////    }
////}

public class DecisionServiceV2(IClearanceDecisionBuilder clearanceDecisionBuilder, ICheckProcessor checkProcessor)
    : IDecisionServiceV2
{
    public IReadOnlyList<(string Mrn, ClearanceDecision Decision)> Process(DecisionContextV2 context)
    {
        return context.CustomsDeclarations.Select(wrapper => Process(context, wrapper)).ToList();
    }

    private (string Mrn, ClearanceDecision Decision) Process(
        DecisionContextV2 context,
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
        DecisionContextV2 context,
        CustomsDeclarationWrapper clearanceRequest,
        Commodity commodity
    )
    {
        var checks = commodity.Checks;
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
