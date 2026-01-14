namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

public static class Constants
{
    public const string Required = "REQUIRED";
}

public static class CommodityRiskResultPhsiDecision
{
    public const string Required = "REQUIRED";
}

public static class ConsignmentDecision
{
    public const string NonAcceptable = "Non Acceptable";
    public const string AcceptableForInternalMarket = "Acceptable for Internal Market";
    public const string AcceptableForNonInternalMarket = "Acceptable for Non Internal Market";
    public const string AcceptableIfChanneled = "Acceptable if Channeled";
    public const string AcceptableForTranshipment = "Acceptable for Transhipment";
    public const string AcceptableForTransit = "Acceptable for Transit";
    public const string AcceptableForTemporaryImport = "Acceptable for Temporary Import";
    public const string AcceptableForSpecificWarehouse = "Acceptable for Specific Warehouse";
    public const string AcceptableForPrivateImport = "Acceptable for Private Import";
    public const string AcceptableForTransfer = "Acceptable for Transfer";
    public const string HorseReEntry = "Horse Re-entry";
}

public static class ControlAuthorityIuuOption
{
    public const string IUUOK = "IUUOK";
    public const string IUUNA = "IUUNA";
    public const string IUUNotCompliant = "IUUNotCompliant";
}

public static class DecisionNotAcceptableAction
{
    public const string Slaughter = "slaughter";
    public const string Reexport = "reexport";
    public const string Euthanasia = "euthanasia";
    public const string Redispatching = "redispatching";
    public const string Destruction = "destruction";
    public const string Transformation = "transformation";
    public const string Other = "other";
    public const string EntryRefusal = "entry-refusal";
    public const string QuarantineImposed = "quarantine-imposed";
    public const string SpecialTreatment = "special-treatment";
    public const string IndustrialProcessing = "industrial-processing";
    public const string ReDispatch = "re-dispatch";
    public const string UseForOtherPurposes = "use-for-other-purposes";
}

public static class ImportNotificationStatus
{
    public const string Draft = "DRAFT";
    public const string Submitted = "SUBMITTED";
    public const string Validated = "VALIDATED";
    public const string Rejected = "REJECTED";
    public const string InProgress = "IN_PROGRESS";
    public const string Amend = "AMEND";
    public const string Modify = "MODIFY";
    public const string Replaced = "REPLACED";
    public const string Cancelled = "CANCELLED";
    public const string Deleted = "DELETED";
    public const string PartiallyRejected = "PARTIALLY_REJECTED";
    public const string SplitConsignment = "SPLIT_CONSIGNMENT";
}

public static class InspectionRequired
{
    public const string Required = "Required";
    public const string Inconclusive = "Inconclusive";
    public const string NotRequired = "Not required";
}