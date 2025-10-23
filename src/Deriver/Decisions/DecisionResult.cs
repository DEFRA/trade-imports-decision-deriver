using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

public record DecisionResult
{
    private readonly List<DocumentDecisionResult> _results = [];

    [SuppressMessage(
        "SonarLint",
        "S107",
        Justification = "To reduce this down to 7 parameters would require quite a bit of refactoring, so for the moment, ignoring this Sonar error"
    )]
    public void AddDecision(
        string mrn,
        int itemNumber,
        string documentReference,
        string? documentCode,
        string? checkCode,
        DecisionCode decisionCode,
        DecisionImportPreNotification? preNotification = null,
        DecisionInternalFurtherDetail? internalDecisionCode = null
    )
    {
        _results.Add(
            new DocumentDecisionResult(
                preNotification,
                mrn,
                itemNumber,
                documentReference,
                documentCode,
                checkCode,
                decisionCode,
                internalDecisionCode
            )
        );
    }

    public IReadOnlyList<DocumentDecisionResult> Decisions => _results.AsReadOnly();
}

[DebuggerDisplay(
    "{ItemNumber} : {DocumentReference} : {DocumentCode} : {CheckCode} : {DecisionCode} : {InternalDecisionCode}"
)]
public record DocumentDecisionResult(
    DecisionImportPreNotification? PreNotification,
    string Mrn,
    int ItemNumber,
    string DocumentReference,
    string? DocumentCode,
    string? CheckCode,
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
            DecisionInternalFurtherDetail.E82 => DocumentDecisionReasons.GmsInspection,
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
            DecisionInternalFurtherDetail.E92 => DocumentDecisionReasons.IuuNotCompliant,
            DecisionInternalFurtherDetail.E93 => DocumentDecisionReasons.IuuAwaitingDecision,
            DecisionInternalFurtherDetail.E94 => DocumentDecisionReasons.IuuDataError,
            DecisionInternalFurtherDetail.E99 => DocumentDecisionReasons.UnknownError,
            _ => null,
        };
    }
}

public record DecisionFinderResult(
    DecisionCode DecisionCode,
    CheckCode? CheckCode,
    DecisionInternalFurtherDetail? InternalDecisionCode = null
);
