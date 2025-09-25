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
                "H220" =>
                    "This customs declaration with a GMS product has been selected for HMI inspection. Either create a new CHED PP or amend an existing one referencing the GMS product. Amend the customs declaration to reference the CHED PP.",
                "H224" =>
                    "Customs declaration clearance withheld. Awaiting IUU check outcome. Contact Port Health Authority (imports) or Marine Management Organisation (landings).",
                _ =>
                    $"CHED reference {DocumentReference} cannot be found in IPAFFS. Check that the reference is correct.",
            },
            DecisionInternalFurtherDetail.E71 =>
                "This CHED has been cancelled. Update the customs declaration with a new reference.",
            DecisionInternalFurtherDetail.E72 =>
                "This CHED has been replaced. Update the customs declaration with a new reference.",
            DecisionInternalFurtherDetail.E73 =>
                "This CHED has been deleted. Update the customs declaration with a new reference",
            DecisionInternalFurtherDetail.E74 =>
                "This consignment needs to be split in IPAFFS, creating an updated CHED reference with either a V or an R at the end.",
            DecisionInternalFurtherDetail.E75 =>
                "Update the customs declaration to reference the new CHED references that have either a V or an R at the end.",
            DecisionInternalFurtherDetail.E84 =>
                "Create a new IPAFFS notification for the correct CHED type. Reference the new CHED on the customs declaration",
            DecisionInternalFurtherDetail.E85 =>
                "Customs declaration states this item requires a PHSI check. IPAFFS has not provided that decision. Contact the National Clearance Hub.",
            DecisionInternalFurtherDetail.E86 =>
                "Customs declaration states this item requires an HMI check. IPAFFS has not provided that decision. Contact the National Clearance Hub.",
            DecisionInternalFurtherDetail.E87 =>
                "This customs declaration with a GMS product has been selected for HMI inspection. Either create a new CHED PP or amend an existing one referencing the GMS product. Amend the customs declaration to reference the CHED PP.",
            DecisionInternalFurtherDetail.E92 =>
                "IUU Not compliant - Contact Port Health Authority (imports) or Marine Management Organisation (landings).",
            DecisionInternalFurtherDetail.E93 => "IUU Awaiting decision",
            DecisionInternalFurtherDetail.E94 => "IUU Data error",
            DecisionInternalFurtherDetail.E99 => "An unknown error has occurred",
            _ => null,
        };
    }
}

public record DecisionFinderResult(
    DecisionCode DecisionCode,
    CheckCode? CheckCode,
    DecisionInternalFurtherDetail? InternalDecisionCode = null
);
