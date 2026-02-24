namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Processors;

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
        return InternalDecisionCode switch
        {
            DecisionInternalFurtherDetail.E70 => CheckCode switch
            {
                "H224" => null,
                _ => DocumentDecisionReasons.ChedNotFound(DocumentReference),
            },
            DecisionInternalFurtherDetail.E71 => DocumentDecisionReasons.CancelledChed,
            DecisionInternalFurtherDetail.E72 => DocumentDecisionReasons.ReplacedChed,
            DecisionInternalFurtherDetail.E73 => DocumentDecisionReasons.DeletedChed,
            DecisionInternalFurtherDetail.E74 => CheckCode switch
            {
                "H218" or "H219" or "H220" => DocumentDecisionReasons.SplitChed,
                _ => null,
            },
            DecisionInternalFurtherDetail.E75 => CheckCode switch
            {
                "H218" or "H219" or "H220" => DocumentDecisionReasons.UpdateCrToReferenceSplitChed,
                _ => null,
            },
            DecisionInternalFurtherDetail.E82 => DocumentDecisionReasons.GmsInspectionAmend,
            DecisionInternalFurtherDetail.E83 => DocumentDecisionReasons.OrphanCheckCode,
            DecisionInternalFurtherDetail.E84 => DocumentDecisionReasons.CreateNewIpaffsNotification,
            DecisionInternalFurtherDetail.E85 => CheckCode switch
            {
                "H219" => DocumentDecisionReasons.PhsiCheckRequired,
                _ => null,
            },
            DecisionInternalFurtherDetail.E86 => CheckCode switch
            {
                "H218" or "H220" => DocumentDecisionReasons.HmiCheckRequired,
                _ => null,
            },
            DecisionInternalFurtherDetail.E87 => DocumentDecisionReasons.GmsInspection,
            DecisionInternalFurtherDetail.E99 => DocumentDecisionReasons.UnknownError,
            _ => null,
        };
    }
}
