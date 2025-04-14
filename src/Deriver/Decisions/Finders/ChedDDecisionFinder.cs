using Btms.Business.Services.Decisions;
using Defra.TradeImportsDataApi.Domain.Ipaffs;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

public class ChedDDecisionFinder : DecisionFinder
{
    public override bool CanFindDecision(ImportPreNotification notification, CheckCode? checkCode) =>
        notification.ImportNotificationType == ImportNotificationType.Ced
        && notification.PartTwo?.ControlAuthority?.IuuCheckRequired != true
        && checkCode?.GetImportNotificationType() == ImportNotificationType.Ced;

    protected override DecisionFinderResult FindDecisionInternal(
        ImportPreNotification notification,
        CheckCode? checkCode
    )
    {
        if (TryGetHoldDecision(notification, out var code))
        {
            return new DecisionFinderResult(code!.Value, checkCode);
        }

        var consignmentAcceptable = notification.PartTwo?.Decision?.ConsignmentAcceptable;
        return consignmentAcceptable switch
        {
            true => notification.PartTwo?.Decision?.ConsignmentDecision switch
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
            },
            false => notification.PartTwo?.Decision?.NotAcceptableAction switch
            {
                DecisionNotAcceptableAction.Destruction => new DecisionFinderResult(DecisionCode.N02, checkCode),
                DecisionNotAcceptableAction.Redispatching => new DecisionFinderResult(DecisionCode.N04, checkCode),
                DecisionNotAcceptableAction.Transformation => new DecisionFinderResult(DecisionCode.N03, checkCode),
                DecisionNotAcceptableAction.Other => new DecisionFinderResult(DecisionCode.N07, checkCode),
                null => HandleNullNotAcceptableAction(notification, checkCode),
                _ => new DecisionFinderResult(
                    DecisionCode.X00,
                    checkCode,
                    InternalDecisionCode: DecisionInternalFurtherDetail.E97
                ),
            },
            _ => new DecisionFinderResult(
                DecisionCode.X00,
                checkCode,
                InternalDecisionCode: DecisionInternalFurtherDetail.E99
            ),
        };
    }
}
