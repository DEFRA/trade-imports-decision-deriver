using Defra.TradeImportsDataApi.Domain.Ipaffs.Constants;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

public class ChedADecisionFinder : DecisionFinder
{
    public override bool CanFindDecision(
        DecisionImportPreNotification notification,
        CheckCode? checkCode,
        string? documentCode
    ) => notification.IuuCheckRequired != true && checkCode != null && checkCode.IsValidDocumentCode(documentCode);

    public override string ChedType => ImportNotificationType.Cveda;

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
                InternalDecisionCode: DecisionInternalFurtherDetail.E74
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
            };
        }

        if (notification.NotAcceptableAction is not null)
        {
            return notification.NotAcceptableAction switch
            {
                DecisionNotAcceptableAction.Euthanasia or DecisionNotAcceptableAction.Slaughter =>
                    new DecisionFinderResult(DecisionCode.N02, checkCode),
                DecisionNotAcceptableAction.Reexport => new DecisionFinderResult(DecisionCode.N04, checkCode),
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
