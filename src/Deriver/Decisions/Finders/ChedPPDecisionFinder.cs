using Defra.TradeImportsDataApi.Domain.Ipaffs.Constants;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

// ReSharper disable once InconsistentNaming
public class ChedPPDecisionFinder : DecisionFinder
{
    public override bool CanFindDecision(
        DecisionImportPreNotification notification,
        CheckCode? checkCode,
        string? documentCode
    ) =>
        notification.ImportNotificationType == ImportNotificationType.Chedpp
        && notification.Status != ImportNotificationStatus.Cancelled
        && notification.Status != ImportNotificationStatus.Replaced
        && checkCode?.GetImportNotificationType() == ImportNotificationType.Chedpp
        && ValidCheckCodeAndDocumentCodeMapping(checkCode, documentCode);

    protected override DecisionFinderResult FindDecisionInternal(
        DecisionImportPreNotification notification,
        CheckCode? checkCode
    )
    {
        return notification.Status switch
        {
            ImportNotificationStatus.Submitted or ImportNotificationStatus.InProgress => new DecisionFinderResult(
                DecisionCode.H02,
                checkCode
            ),
            ImportNotificationStatus.Validated or ImportNotificationStatus.Rejected => checkCode?.Value switch
            {
                "H218" or "H220" => ProcessHmi(notification, checkCode),
                "H219" => ProcessPhsi(notification, checkCode),
                _ => new DecisionFinderResult(
                    DecisionCode.X00,
                    checkCode,
                    InternalDecisionCode: DecisionInternalFurtherDetail.E99
                ),
            },
            ImportNotificationStatus.PartiallyRejected => new DecisionFinderResult(DecisionCode.H01, checkCode),
            _ => new DecisionFinderResult(
                DecisionCode.X00,
                checkCode,
                InternalDecisionCode: DecisionInternalFurtherDetail.E99
            ),
        };
    }

    private static DecisionFinderResult ProcessHmi(DecisionImportPreNotification notification, CheckCode? checkCode)
    {
        var hmiCheck = notification.CommodityChecks.FirstOrDefault(x => x.Type == "HMI");

        if (hmiCheck is null)
        {
            return new DecisionFinderResult(
                DecisionCode.H01,
                checkCode,
                InternalDecisionCode: DecisionInternalFurtherDetail.E86
            );
        }

        return GetDecisionFromStatus(hmiCheck.Status, checkCode);
    }

    private static DecisionFinderResult ProcessPhsi(DecisionImportPreNotification notification, CheckCode? checkCode)
    {
        var documentCheck = notification.CommodityChecks.FirstOrDefault(x => x.Type == "PHSI_DOCUMENT");
        var physicalCheck = notification.CommodityChecks.FirstOrDefault(x => x.Type == "PHSI_PHYSICAL");
        var identifyCheck = notification.CommodityChecks.FirstOrDefault(x => x.Type == "PHSI_IDENTITY");

        if (documentCheck is null || physicalCheck is null || identifyCheck is null)
        {
            return new DecisionFinderResult(
                DecisionCode.H01,
                checkCode,
                InternalDecisionCode: DecisionInternalFurtherDetail.E85
            );
        }

        var decisions = new List<DecisionFinderResult>
        {
            GetDecisionFromStatus(documentCheck.Status, checkCode),
            GetDecisionFromStatus(physicalCheck.Status, checkCode),
            GetDecisionFromStatus(identifyCheck.Status, checkCode),
        };

        return decisions.OrderByDescending(x => x.DecisionCode).First();
    }

    private static DecisionFinderResult GetDecisionFromStatus(string status, CheckCode? checkCode)
    {
        return status switch
        {
            "To do" or "Hold" => new DecisionFinderResult(DecisionCode.H01, checkCode),
            "To be inspected" => new DecisionFinderResult(DecisionCode.H02, checkCode),
            "Compliant" or "Auto cleared" => new DecisionFinderResult(DecisionCode.C03, checkCode),
            "Non compliant" => new DecisionFinderResult(DecisionCode.N01, checkCode),
            "Not inspected" => new DecisionFinderResult(DecisionCode.C02, checkCode),
            _ => new DecisionFinderResult(
                DecisionCode.X00,
                checkCode,
                InternalDecisionCode: DecisionInternalFurtherDetail.E99
            ),
        };
    }

    private static bool ValidCheckCodeAndDocumentCodeMapping(CheckCode? checkCode, string? documentCode)
    {
        return (checkCode?.Value == "H219" && documentCode is "N851" or "9115")
            || (checkCode?.Value is "H218" or "H220" && documentCode is "N002");
    }
}
