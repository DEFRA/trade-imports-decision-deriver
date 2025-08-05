namespace Defra.TradeImportsDecisionDeriver.Deriver.Matching;

public record MatchingResult
{
    private readonly List<Match> _matches = [];
    private readonly List<DocumentNoMatch> _noMatches = [];

    public void AddMatch(
        string notificationId,
        string mrn,
        int itemNumber,
        string documentReference,
        string? documentCode
    )
    {
        if (
            !_matches.Exists(x =>
                x.ImportPreNotificationId == notificationId
                && x.Mrn == mrn
                && x.ItemNumber == itemNumber
                && x.DocumentReference == documentReference
                && x.DocumentCode == documentCode
            )
        )
            _matches.Add(new Match(notificationId, mrn, itemNumber, documentReference, documentCode));
    }

    public void AddDocumentNoMatch(string mrn, int itemNumber, string documentReference, string? documentCode)
    {
        _noMatches.Add(new DocumentNoMatch(mrn, itemNumber, documentReference, documentCode));
    }

    public IReadOnlyList<Match> Matches => _matches.AsReadOnly();

    public IReadOnlyList<DocumentNoMatch> NoMatches => _noMatches.AsReadOnly();
}

public record Match(
    string ImportPreNotificationId,
    string Mrn,
    int ItemNumber,
    string DocumentReference,
    string? DocumentCode
);

public record DocumentNoMatch(string Mrn, int ItemNumber, string DocumentReference, string? DocumentCode);
