using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Ipaffs.Constants;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

public interface ICheckCodeDecisionFinder
{
    DecisionFinderResult[] FindDecision(DecisionImportPreNotification notification, CheckCode checkCode);
}

public class H221DecisionFinder : ICheckCodeDecisionFinder
{
    public DecisionFinderResult[] FindDecision(DecisionContext context, Commodity commodity, CheckCode checkCode)
    {
        var documentCodes = checkCode.GetDocumentCodes();
        if (commodity.Documents != null)
        {
            var documents = commodity.Documents.Where(x => documentCodes.Contains(x.DocumentCode));
        }

        context.Notifications.
        
        if (notification.Status == ImportNotificationStatus.PartiallyRejected)
        {
            return new[]
            {
                new DecisionFinderResult(
                    DecisionCode.X00,
                    checkCode,
                    InternalDecisionCode: DecisionInternalFurtherDetail.E74
                )
            };
        }
        return Array.Empty<DecisionFinderResult>();
    }
}

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

        return GetDocumentCodes().Contains(documentCode);
    }

    public string[] GetDocumentCodes()
    {
        return Value switch
        {
            "H218" or "H220" => ["C085", "N002"],
            "H219" =>  ["C085" , "9115" , "N851"],
            "H221" =>  ["C640"],
            "H222" or "H224" =>  ["N853"],
            "H223" => ["C678" , "N852"],
            _ => [],
        };
    }

    public override string ToString() => Value;
}
