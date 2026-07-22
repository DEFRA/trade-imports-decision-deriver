using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Trade.Gateway.Api.Contract.Certificate;

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

            var cheds = FindTrachesCheds(context.Cheds, documentIdentifier!);

            var notifications = FindDecisionImportPreNotification(
                context.Notifications,
                documentCode,
                documentIdentifier!
            );

            var decisionImportPreNotifications =
                notifications as DecisionImportPreNotification[] ?? notifications.ToArray();
            if (decisionImportPreNotifications.Any() || cheds.Any())
            {
                output.AddRange(
                    ProcessNotification(context, clearanceRequest, commodity, notifications, checkCode, document, cheds)
                );
            }
            else
            {
                var resolverContext = new DecisionEngineContext(
                    context,
                    null!,
                    clearanceRequest,
                    commodity,
                    checkCode,
                    document,
                    null
                );

                var result = RunEngine("UNKNOWN", checkCode, resolverContext);
                output.Add(
                    new CheckDecisionResult(
                        null,
                        null!,
                        clearanceRequest.MovementReferenceNumber,
                        commodity.ItemNumber!.Value,
                        document.DocumentReference?.Value,
                        documentCode,
                        checkCodeValue,
                        result.Code,
                        result.RuleName,
                        result.Mode,
                        result.Level,
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
                null,
                null
            );

            var result = RunEngine("UNKNOWN", checkCode, resolverContext);
            output.Add(
                new CheckDecisionResult(
                    null,
                    null!,
                    clearanceRequest.MovementReferenceNumber,
                    commodity.ItemNumber!.Value,
                    string.Empty,
                    null,
                    checkCodeValue,
                    result.Code,
                    result.RuleName,
                    result.Mode,
                    result.Level,
                    result.FurtherDetail
                )
            );
        }

        return output.Distinct().ToList();
    }

    private List<CheckDecisionResult> ProcessNotification(
        DecisionContext context,
        CustomsDeclarationWrapper clearanceRequest,
        Commodity commodity,
        IEnumerable<DecisionImportPreNotification> notifications,
        CheckCode checkCode,
        ImportDocument document,
        ////DecisionRulesEngine decisionEngine,
        IEnumerable<DefraUNVTDCHEDProfile> cheds
    )
    {
        var output = new List<CheckDecisionResult>();
        if (cheds.Any())
        {
            foreach (var ched in cheds)
            {
                var resolverContext = new DecisionEngineContext(
                    context,
                    null!,
                    clearanceRequest,
                    commodity,
                    checkCode,
                    document,
                    ched
                );

                var result = RunEngine("TRACES", checkCode, resolverContext);
                output.Add(
                    new CheckDecisionResult(
                        null,
                        ched,
                        clearanceRequest.MovementReferenceNumber,
                        commodity.ItemNumber!.Value,
                        document.DocumentReference?.Value,
                        document.DocumentCode,
                        checkCode.Value,
                        result.Code,
                        result.RuleName,
                        result.Mode,
                        result.Level,
                        result.FurtherDetail
                    )
                );

                if (result.PassiveResults != null)
                {
                    foreach (var passiveResult in result.PassiveResults)
                    {
                        output.Add(
                            new CheckDecisionResult(
                                null,
                                ched,
                                clearanceRequest.MovementReferenceNumber,
                                commodity.ItemNumber!.Value,
                                document.DocumentReference?.Value,
                                document.DocumentCode,
                                checkCode.Value,
                                passiveResult.Code,
                                passiveResult.RuleName,
                                passiveResult.Mode,
                                passiveResult.Level,
                                passiveResult.FurtherDetail
                            )
                        );
                    }
                }
            }
        }
        else
        {
            foreach (var notification in notifications)
            {
                var resolverContext = new DecisionEngineContext(
                    context,
                    notification,
                    clearanceRequest,
                    commodity,
                    checkCode,
                    document,
                    null
                );

                var result = RunEngine("IPAFFS", checkCode, resolverContext);
                output.Add(
                    new CheckDecisionResult(
                        notification,
                        null,
                        clearanceRequest.MovementReferenceNumber,
                        commodity.ItemNumber!.Value,
                        document.DocumentReference?.Value,
                        document.DocumentCode,
                        checkCode.Value,
                        result.Code,
                        result.RuleName,
                        result.Mode,
                        result.Level,
                        result.FurtherDetail
                    )
                );

                if (result.PassiveResults != null)
                {
                    foreach (var passiveResult in result.PassiveResults)
                    {
                        output.Add(
                            new CheckDecisionResult(
                                notification,
                                null,
                                clearanceRequest.MovementReferenceNumber,
                                commodity.ItemNumber!.Value,
                                document.DocumentReference?.Value,
                                document.DocumentCode,
                                checkCode.Value,
                                passiveResult.Code,
                                passiveResult.RuleName,
                                passiveResult.Mode,
                                passiveResult.Level,
                                passiveResult.FurtherDetail
                            )
                        );
                    }
                }
            }
        }

        return output;
    }

    private DecisionEngineResult RunEngine(string source, CheckCode checkCode, DecisionEngineContext context)
    {
        var decisionEngine = decisionRulesEngineFactory.Get(source, checkCode.GetImportNotificationType());
        return decisionEngine.Run(context);
    }

    private static IEnumerable<DecisionImportPreNotification> FindDecisionImportPreNotification(
        List<DecisionImportPreNotification> notifications,
        string documentCode,
        string documentIdentifier
    )
    {
        return from candidate in notifications
            let candidateIdentifier = new ImportDocumentReference(candidate.Id!).GetIdentifier(documentCode)
            where candidateIdentifier == documentIdentifier
            select candidate;
    }

    private static IEnumerable<DefraUNVTDCHEDProfile> FindTrachesCheds(
        List<DefraUNVTDCHEDProfile> cheds,
        string documentIdentifier
    )
    {
        return from candidate in cheds
            where candidate.ExchangedDocument.Identifier == documentIdentifier
            select candidate;
    }
}
