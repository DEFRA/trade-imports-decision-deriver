using Defra.TradeImportsDataApi.Domain.Ipaffs.Constants;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

public class ChedDDecisionFinder : DecisionFinder
{
    public override bool CanFindDecision(DecisionImportPreNotification notification, CheckCode? checkCode) =>
        notification.ImportNotificationType == ImportNotificationType.Ced
        && notification.IuuCheckRequired != true
        && checkCode?.GetImportNotificationType() == ImportNotificationType.Ced;

    protected override DecisionFinderResult FindDecisionInternal(
        DecisionImportPreNotification notification,
        CheckCode? checkCode
    )
    {
        if (notification.Status == ImportNotificationStatus.PartiallyRejected)
        {
            return new DecisionFinderResult(
                DecisionCode.X00,
                checkCode,
                InternalDecisionCode: DecisionInternalFurtherDetail.E80
            );
        }

        if (TryGetHoldDecision(notification, out var code))
        {
            return new DecisionFinderResult(code!.Value, checkCode);
        }

        if (notification.HasAcceptableConsignmentDecision())
        {
            return notification.ConsignmentDecision switch
            {
                ConsignmentDecision.AcceptableForInternalMarket => new DecisionFinderResult(
                    DecisionCode.C03,
                    checkCode
                ),
                _ => new DecisionFinderResult(
                    DecisionCode.X00,
                    checkCode,
                    InternalDecisionCode: DecisionInternalFurtherDetail.E96
                ),
            };
        }

        if (notification.NotAcceptableAction is not null)
        {
            return notification.NotAcceptableAction switch
            {
                DecisionNotAcceptableAction.Destruction => new DecisionFinderResult(DecisionCode.N02, checkCode),
                DecisionNotAcceptableAction.Redispatching => new DecisionFinderResult(DecisionCode.N04, checkCode),
                DecisionNotAcceptableAction.Transformation => new DecisionFinderResult(DecisionCode.N03, checkCode),
                DecisionNotAcceptableAction.Other => new DecisionFinderResult(DecisionCode.N07, checkCode),
                _ => new DecisionFinderResult(
                    DecisionCode.X00,
                    checkCode,
                    InternalDecisionCode: DecisionInternalFurtherDetail.E97
                ),
            };
        }

        if (notification.NotAcceptableReasons?.Length > 0)
        {
            return new DecisionFinderResult(DecisionCode.N04, checkCode);
        }

        return new DecisionFinderResult(
            DecisionCode.X00,
            checkCode,
            InternalDecisionCode: DecisionInternalFurtherDetail.E99
        );
    }
}
