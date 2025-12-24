using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.Processors;

public interface ICheckProcessor
{
    IEnumerable<CheckDecisionResult> Process(
        DecisionContextV2 context,
        CustomsDeclarationWrapper clearanceRequest,
        Commodity commodity,
        CommodityCheck check
    );
}

public class CheckProcessor(IDecisionRulesEngineFactory decisionRulesEngineFactory) : ICheckProcessor
{
    private static readonly ImportDocument[] s_emptyDocuments = Array.Empty<ImportDocument>();

    public IEnumerable<CheckDecisionResult> Process(
        DecisionContextV2 context,
        CustomsDeclarationWrapper clearanceRequest,
        Commodity commodity,
        CommodityCheck check
    )
    {
        var documents = commodity.Documents ?? s_emptyDocuments;
        var checkCodeValue = check.CheckCode!;
        var checkCode = new CheckCode { Value = checkCodeValue };

        var output = new List<CheckDecisionResult>(documents.Length);
        bool foundValidDocument = false;
        var decisionEngine = decisionRulesEngineFactory.Get(checkCode.GetImportNotificationType());

        for (int i = 0; i < documents.Length; i++)
        {
            var document = documents[i];

            if (!checkCode.IsValidDocumentCode(document.DocumentCode) || !document.HasValidDocumentReference())
            {
                continue;
            }

            foundValidDocument = true;

            var documentCode = document.DocumentCode!;
            var documentIdentifier = document.GetDocumentReferenceIdentifier();

            var notifications = FindDecisionImportPreNotification(
                context.Notifications,
                documentCode,
                documentIdentifier!
            );

            foreach (var notification in notifications)
            {
                var resolverContext = new DecisionResolutionContext(
                    context,
                    notification,
                    clearanceRequest,
                    commodity,
                    checkCode,
                    document
                );

                var result = decisionEngine.Resolve(resolverContext);
                output.Add(
                    new CheckDecisionResult(
                        notification,
                        clearanceRequest.MovementReferenceNumber,
                        commodity.ItemNumber!.Value,
                        document.DocumentReference?.Value,
                        documentCode,
                        checkCodeValue,
                        result.Code,
                        result.FurtherDetail
                    )
                );
            }
        }

        if (!foundValidDocument)
        {
            var resolverContext = new DecisionResolutionContext(
                context,
                null!,
                clearanceRequest,
                commodity,
                checkCode,
                null
            );

            var result = decisionEngine.Resolve(resolverContext);
            output.Add(
                new CheckDecisionResult(
                    null,
                    clearanceRequest.MovementReferenceNumber,
                    commodity.ItemNumber!.Value,
                    string.Empty,
                    null,
                    checkCodeValue,
                    result.Code,
                    result.FurtherDetail
                )
            );
        }

        return output;
    }

    private static IEnumerable<DecisionImportPreNotification> FindDecisionImportPreNotification(
        List<DecisionImportPreNotification> notifications,
        string documentCode,
        string documentIdentifier
    )
    {
        // Manual loop instead of List.Find + lambda
        for (int i = 0; i < notifications.Count; i++)
        {
            var candidate = notifications[i];

            var candidateIdentifier = new ImportDocumentReference(candidate.Id!).GetIdentifier(documentCode);

            if (candidateIdentifier == documentIdentifier)
            {
                yield return candidate;
            }
        }
    }
}
