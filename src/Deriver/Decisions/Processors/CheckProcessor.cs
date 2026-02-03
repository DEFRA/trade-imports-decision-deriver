using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Processors;

public interface ICheckProcessor
{
    IEnumerable<CheckDecisionResult> Process(
        DecisionContext context,
        CustomsDeclarationWrapper clearanceRequest,
        Commodity commodity,
        CommodityCheck check
    );
}

public class CheckProcessor(IDecisionRulesEngineFactory decisionRulesEngineFactory) : ICheckProcessor
{
    private static readonly ImportDocument[] s_emptyDocuments = Array.Empty<ImportDocument>();

    public IEnumerable<CheckDecisionResult> Process(
        DecisionContext context,
        CustomsDeclarationWrapper clearanceRequest,
        Commodity commodity,
        CommodityCheck check
    )
    {
        var documents = commodity.Documents ?? s_emptyDocuments;
        var checkCodeValue = check.CheckCode!;
        var checkCode = new CheckCode { Value = checkCodeValue };

        var output = new List<CheckDecisionResult>(documents.Length);
        var foundValidDocument = false;
        var decisionEngine = decisionRulesEngineFactory.Get(checkCode.GetImportNotificationType());

        for (var i = 0; i < documents.Length; i++)
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

            var decisionImportPreNotifications =
                notifications as DecisionImportPreNotification[] ?? notifications.ToArray();
            if (!decisionImportPreNotifications.Any())
            {
                foreach (var notification in notifications)
                {
                    var resolverContext = new DecisionEngineContext(
                        context,
                        notification,
                        clearanceRequest,
                        commodity,
                        checkCode,
                        document
                    );

                    var result = decisionEngine.Run(resolverContext);
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
            else
            {
                var resolverContext = new DecisionEngineContext(
                    context,
                    null!,
                    clearanceRequest,
                    commodity,
                    checkCode,
                    document
                );

                var result = decisionEngine.Run(resolverContext);
                output.Add(
                    new CheckDecisionResult(
                        null,
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

            foreach (var notification in decisionImportPreNotifications)
            {
                var resolverContext = new DecisionResolutionContext(
                    context,
                    notification,
                    clearanceRequest,
                    commodity,
                    checkCode,
                    document
                );

                var result = decisionEngine.Run(resolverContext);
                output.Add(
                    new CheckDecisionResult(
                        null,
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
            var resolverContext = new DecisionEngineContext(
                context,
                null!,
                clearanceRequest,
                commodity,
                checkCode,
                null
            );

            var result = decisionEngine.Run(resolverContext);
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

        return output.Distinct().ToList();
    }

    private static IEnumerable<DecisionImportPreNotification> FindDecisionImportPreNotification(
        List<DecisionImportPreNotification> notifications,
        string documentCode,
        string documentIdentifier
    )
    {
        // Manual loop instead of List.Find + lambda
        for (var i = 0; i < notifications.Count; i++)
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
