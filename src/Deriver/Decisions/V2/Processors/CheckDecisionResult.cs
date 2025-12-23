namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.Processors;

public record CheckDecisionResult(
    DecisionImportPreNotification? PreNotification,
    string Mrn,
    int ItemNumber,
    string? DocumentReference,
    string? DocumentCode,
    string CheckCode,
    DecisionCode DecisionCode,
    DecisionInternalFurtherDetail? InternalDecisionCode = null
)
{
    public string? DecisionReason => GetDecisionReason();

    private string? GetDecisionReason()
    {
        return new DocumentDecisionResult(
            PreNotification,
            Mrn,
            ItemNumber,
            DocumentReference!,
            DocumentCode,
            CheckCode,
            DecisionCode,
            InternalDecisionCode
        ).DecisionReason;
    }
}
