using Defra.TradeImportsDataApi.Domain.Ipaffs;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

public class ChedADecisionFinder : DecisionFinder
{
    public override bool CanFindDecision(ImportPreNotification notification, CheckCode? checkCode) =>
        notification.ImportNotificationType == ImportNotificationType.Cveda
        && notification.PartTwo?.ControlAuthority?.IuuCheckRequired != true;

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
                ConsignmentDecision.AcceptableForTranshipment or ConsignmentDecision.AcceptableForTransit =>
                    new DecisionFinderResult(DecisionCode.E03, checkCode),
                ConsignmentDecision.AcceptableForInternalMarket => new DecisionFinderResult(
                    DecisionCode.C03,
                    checkCode
                ),
                ConsignmentDecision.AcceptableForTemporaryImport => new DecisionFinderResult(
                    DecisionCode.C05,
                    checkCode
                ),
                ConsignmentDecision.HorseReEntry => new DecisionFinderResult(DecisionCode.C06, checkCode),
                _ => new DecisionFinderResult(
                    DecisionCode.X00,
                    checkCode,
                    InternalDecisionCode: DecisionInternalFurtherDetail.E96
                ),
            },
            false => notification.PartTwo?.Decision?.NotAcceptableAction switch
            {
                DecisionNotAcceptableAction.Euthanasia or DecisionNotAcceptableAction.Slaughter =>
                    new DecisionFinderResult(DecisionCode.N02, checkCode),
                DecisionNotAcceptableAction.Reexport => new DecisionFinderResult(DecisionCode.N04, checkCode),
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
