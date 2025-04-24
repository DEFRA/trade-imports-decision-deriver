using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

public record DecisionResult
{
    private readonly List<DocumentDecisionResult> _results = [];

    public void AddDecision(
        string mrn,
        int itemNumber,
        string documentReference,
        string? checkCode,
        DecisionCode decisionCode,
        DecisionImportPreNotification? preNotification = null,
        string? decisionReason = null,
        DecisionInternalFurtherDetail? internalDecisionCode = null
    )
    {
        _results.Add(
            new DocumentDecisionResult(
                preNotification,
                mrn,
                itemNumber,
                documentReference,
                checkCode,
                decisionCode,
                decisionReason,
                internalDecisionCode
            )
        );
    }

    public IReadOnlyList<DocumentDecisionResult> Decisions => _results.AsReadOnly();
}

public record DocumentDecisionResult(
    DecisionImportPreNotification? PreNotification,
    string Mrn,
    int ItemNumber,
    string DocumentReference,
    string? CheckCode,
    DecisionCode DecisionCode,
    string? DecisionReason,
    DecisionInternalFurtherDetail? InternalDecisionCode = null
);

public record DecisionFinderResult(
    DecisionCode DecisionCode,
    CheckCode? CheckCode,
    string? DecisionReason = null,
    DecisionInternalFurtherDetail? InternalDecisionCode = null
);
