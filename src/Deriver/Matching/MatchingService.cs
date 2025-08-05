using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Matching;

public class MatchingService : IMatchingService
{
    public Task<MatchingResult> Process(MatchingContext matchingContext, CancellationToken cancellationToken)
    {
        var matchingResult = new MatchingResult();
        foreach (var wrapper in matchingContext.ClearanceRequests)
        {
            if (wrapper.ClearanceRequest.Commodities == null)
                continue;
            foreach (var item in wrapper.ClearanceRequest.Commodities)
            {
                if (item.Documents == null)
                    continue;

                var groupedDocuments = item
                    .Documents.GroupBy(d => new { d.DocumentReference, d.DocumentCode })
                    .Select(d => d.Key);

                foreach (var documentGroup in groupedDocuments)
                {
                    ProcessDocument(
                        matchingContext,
                        documentGroup.DocumentReference,
                        documentGroup.DocumentCode,
                        wrapper,
                        item,
                        matchingResult
                    );
                }
            }
        }

        return Task.FromResult(matchingResult);
    }

    private static void ProcessDocument(
        MatchingContext matchingContext,
        ImportDocumentReference? documentGroup,
        string? documentCode,
        ClearanceRequestWrapper wrapper,
        Commodity item,
        MatchingResult matchingResult
    )
    {
        if (documentGroup == null || !ImportDocumentReference.IsValid(documentCode!))
            return;

        var notification = matchingContext.Notifications.Find(x =>
            new ImportDocumentReference(x.Id!).GetIdentifier(documentCode!)
            == documentGroup.GetIdentifier(documentCode!)
        );

        if (notification is null)
        {
            matchingResult.AddDocumentNoMatch(
                wrapper.MovementReferenceNumber,
                item.ItemNumber!.Value,
                documentGroup.Value,
                documentCode
            );
        }
        else
        {
            matchingResult.AddMatch(
                notification.Id!,
                wrapper.MovementReferenceNumber,
                item.ItemNumber!.Value,
                documentGroup.Value,
                documentCode
            );
        }
    }
}
