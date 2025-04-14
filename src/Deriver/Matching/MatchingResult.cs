namespace Defra.TradeImportsDecisionDeriver.Deriver.Matching;

public record MatchingResult
{
    private readonly List<Match> _matches = [];
    private readonly List<DocumentNoMatch> _noMatches = [];

    public void AddMatch(string notificationId, string mrn, int itemNumber, string documentReference)
    {
        _matches.Add(new Match(notificationId, mrn, itemNumber, documentReference));
    }

    public void AddDocumentNoMatch(string mrn, int itemNumber, string documentReference)
    {
        _noMatches.Add(new DocumentNoMatch(mrn, itemNumber, documentReference));
    }

    public IReadOnlyList<Match> Matches => _matches.AsReadOnly();

    public IReadOnlyList<DocumentNoMatch> NoMatches => _noMatches.AsReadOnly();
}

public record Match(string ImportPreNotificationId, string Mrn, int ItemNumber, string DocumentReference);

public record DocumentNoMatch(string Mrn, int ItemNumber, string DocumentReference);
