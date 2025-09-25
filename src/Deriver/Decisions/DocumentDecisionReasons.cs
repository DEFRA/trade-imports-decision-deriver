namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

public static class DocumentDecisionReasons
{
    public static string ChedNotFound(string? documentReference) =>
        $"CHED reference {documentReference} cannot be found in IPAFFS. Check that the reference is correct.";

    public const string IuuAwaitingOutcome =
        "Customs declaration clearance withheld. Awaiting IUU check outcome. Contact Port Health Authority (imports) or Marine Management Organisation (landings).";

    public const string IuuAwaitingDecision = "IUU Awaiting decision.";

    public const string IuuDataError = "IUU Data error.";

    public const string IuuNotCompliant =
        "IUU Not compliant - Contact Port Health Authority (imports) or Marine Management Organisation (landings).";

    public const string UnknownError = "An unknown error has occurred.";

    public const string GmsInspection =
        "This customs declaration with a GMS product has been selected for HMI inspection. Either create a new CHED PP or amend an existing one referencing the GMS product. Amend the customs declaration to reference the CHED PP..";

    public const string CancelledChed =
        "This CHED has been cancelled. Update the customs declaration with a new reference.";

    public const string ReplacedChed =
        "This CHED has been replaced. Update the customs declaration with a new reference.";

    public const string DeletedChed =
        "This CHED has been deleted. Update the customs declaration with a new reference.";

    public const string SplitChed =
        "This consignment needs to be split in IPAFFS, creating an updated CHED reference with either a V or an R at the end.";

    public const string UpdateCrToReferenceSplitChed =
        "Update the customs declaration to reference the new CHED references that have either a V or an R at the end.";

    public const string CreateNewIpaffsNotification =
        "Create a new IPAFFS notification for the correct CHED type. Reference the new CHED on the customs declaration";

    public const string PhsiCheckRequired =
        "Customs declaration states this item requires a PHSI check. IPAFFS has not provided that decision. Contact the National Clearance Hub.";

    public const string HmiCheckRequired =
        "Customs declaration states this item requires an HMI check. IPAFFS has not provided that decision. Contact the National Clearance Hub.";
}
