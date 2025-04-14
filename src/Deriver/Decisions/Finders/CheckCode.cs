using Defra.TradeImportsDataApi.Domain.Ipaffs;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

public class CheckCode
{
    public const string IuuCheckCode = "H224";

    public required string Value { get; set; }

    public ImportNotificationType? GetImportNotificationType()
    {
        //This is the mapping from https://eaflood.atlassian.net/wiki/spaces/ALVS/pages/5400920093/DocumentCode+CheckCode+Mapping
        return Value switch
        {
            "H221" => ImportNotificationType.Cveda,
            "H223" => ImportNotificationType.Ced,
            "H222" or "H224" => ImportNotificationType.Cvedp,
            "H219" or "H218" or "H220" => ImportNotificationType.Chedpp,
            _ => null,
        };
    }

    public bool IsIuu()
    {
        return Value == IuuCheckCode;
    }
}