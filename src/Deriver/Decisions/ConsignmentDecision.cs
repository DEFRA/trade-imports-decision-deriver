namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

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
