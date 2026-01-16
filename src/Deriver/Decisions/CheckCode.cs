using Defra.TradeImportsDataApi.Domain.Ipaffs.Constants;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

public class CheckCode
{
    public const string IuuCheckCode = "H224";

    public required string Value { get; set; }

    public string? GetImportNotificationType()
    {
        // This is the mapping from https://eaflood.atlassian.net/wiki/spaces/ALVS/pages/5400920093/DocumentCode+CheckCode+Mapping
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

    public bool IsValidDocumentCode(string? documentCode)
    {
        if (string.IsNullOrEmpty(documentCode))
        {
            return false;
        }

        return Value switch
        {
            "H218" or "H220" => documentCode is "C085" or "N002",
            "H219" => documentCode is "C085" or "9115" or "N851",
            "H221" => documentCode is "C640",
            "H222" or "H224" => documentCode is "N853",
            "H223" => documentCode is "C678" or "N852",
            _ => false,
        };
    }

    public override string ToString() => Value;
}
