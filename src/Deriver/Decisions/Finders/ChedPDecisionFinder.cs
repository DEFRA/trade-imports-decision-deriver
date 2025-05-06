using Defra.TradeImportsDataApi.Domain.Ipaffs.Constants;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

public class ChedPDecisionFinder : DecisionFinder
{
    public override bool CanFindDecision(DecisionImportPreNotification notification, CheckCode? checkCode) =>
        notification.ImportNotificationType == ImportNotificationType.Cvedp
        && (
            (checkCode is null)
            || (checkCode.GetImportNotificationType() == ImportNotificationType.Cvedp && !checkCode.IsIuu())
        );

    protected override DecisionFinderResult FindDecisionInternal(
        DecisionImportPreNotification notification,
        CheckCode? checkCode
    )
    {
        if (TryGetHoldDecision(notification, out var code))
        {
            return new DecisionFinderResult(code!.Value, checkCode);
        }

        if (
            !TryGetConsignmentAcceptable(
                notification,
                out var consignmentAcceptable,
                out var decisionCode,
                out var internalDecisionCode
            )
        )
        {
            return new DecisionFinderResult(decisionCode!.Value, checkCode, InternalDecisionCode: internalDecisionCode);
        }

        return consignmentAcceptable switch
        {
            true => notification.ConsignmentDecision switch
            {
                ConsignmentDecision.AcceptableForTranshipment
                or ConsignmentDecision.AcceptableForTransit
                or ConsignmentDecision.AcceptableForSpecificWarehouse => new DecisionFinderResult(
                    DecisionCode.E03,
                    checkCode
                ),
                ConsignmentDecision.AcceptableForInternalMarket => new DecisionFinderResult(
                    DecisionCode.C03,
                    checkCode
                ),
                ConsignmentDecision.AcceptableIfChanneled => new DecisionFinderResult(DecisionCode.C06, checkCode),
                _ => new DecisionFinderResult(
                    DecisionCode.X00,
                    checkCode,
                    InternalDecisionCode: DecisionInternalFurtherDetail.E96
                ),
            },
            false => notification.NotAcceptableAction switch
            {
                DecisionNotAcceptableAction.Destruction => new DecisionFinderResult(DecisionCode.N02, checkCode),
                DecisionNotAcceptableAction.Reexport => new DecisionFinderResult(DecisionCode.N04, checkCode),
                DecisionNotAcceptableAction.Transformation => new DecisionFinderResult(DecisionCode.N03, checkCode),
                DecisionNotAcceptableAction.Other => new DecisionFinderResult(DecisionCode.N07, checkCode),
                null => HandleNullNotAcceptableAction(notification, checkCode),
                _ => new DecisionFinderResult(
                    DecisionCode.X00,
                    checkCode,
                    InternalDecisionCode: DecisionInternalFurtherDetail.E97
                ),
            },
        };
    }

    private static bool TryGetConsignmentAcceptable(
        DecisionImportPreNotification notification,
        out bool acceptable,
        out DecisionCode? decisionCode,
        out DecisionInternalFurtherDetail? internalDecisionCode
    )
    {
        var consignmentAcceptable = notification.ConsignmentAcceptable;
        decisionCode = null;
        internalDecisionCode = null;
        acceptable = false;

        if (consignmentAcceptable.HasValue)
        {
            acceptable = consignmentAcceptable.Value;
            return true;
        }

        if (notification.AutoClearedOn.HasValue)
        {
            acceptable = true;
            return true;
        }

        decisionCode = DecisionCode.X00;
        internalDecisionCode = DecisionInternalFurtherDetail.E99;
        return false;
    }
}
