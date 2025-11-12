using Defra.TradeImportsDataApi.Domain.Ipaffs.Constants;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

public class IuuDecisionFinder : DecisionFinder
{
    public override bool CanFindDecision(
        DecisionImportPreNotification notification,
        CheckCode? checkCode,
        string? documentCode
    ) =>
        checkCode?.GetImportNotificationType() == ChedType
        && checkCode.IsValidDocumentCode(documentCode)
        && checkCode.IsIuu();

    protected override DecisionFinderResult FindDecisionInternal(
        DecisionImportPreNotification notification,
        CheckCode? checkCode
    )
    {
        return (notification.IuuCheckRequired == true) switch
        {
            true => notification.IuuOption switch
            {
                ControlAuthorityIuuOption.IUUOK => new DecisionFinderResult(DecisionCode.C07, checkCode),
                ControlAuthorityIuuOption.IUUNotCompliant => new DecisionFinderResult(DecisionCode.X00, checkCode),
                ControlAuthorityIuuOption.IUUNA => new DecisionFinderResult(DecisionCode.C08, checkCode),
                _ => new DecisionFinderResult(DecisionCode.H02, checkCode, DecisionInternalFurtherDetail.E93),
            },
            false => new DecisionFinderResult(DecisionCode.H02, checkCode, DecisionInternalFurtherDetail.E94),
        };
    }

    public override string ChedType => ImportNotificationType.Cvedp;
}
