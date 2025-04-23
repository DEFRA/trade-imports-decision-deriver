using Defra.TradeImportsDataApi.Domain.Ipaffs;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

// ReSharper disable once InconsistentNaming
public class ChedPPDecisionFinder : DecisionFinder
{
    public override bool CanFindDecision(DecisionImportPreNotification notification, CheckCode? checkCode) =>
        notification.ImportNotificationType == ImportNotificationType.Chedpp
        && notification.Status != ImportNotificationStatus.Cancelled
        && notification.Status != ImportNotificationStatus.Replaced
        && checkCode?.GetImportNotificationType() == ImportNotificationType.Chedpp;

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
            ImportNotificationStatus.Validated => new DecisionFinderResult(DecisionCode.C03, checkCode),
            ImportNotificationStatus.Rejected => new DecisionFinderResult(DecisionCode.N02, checkCode),
            ImportNotificationStatus.PartiallyRejected => new DecisionFinderResult(DecisionCode.H01, checkCode),
            _ => new DecisionFinderResult(
                DecisionCode.X00,
                checkCode,
                InternalDecisionCode: DecisionInternalFurtherDetail.E99
            ),
        };
    }
}
