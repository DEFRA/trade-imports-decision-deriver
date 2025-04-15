using Defra.TradeImportsDataApi.Domain.Ipaffs;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;

public class IuuDecisionFinder : DecisionFinder
{
    public override bool CanFindDecision(ImportPreNotification notification, CheckCode? checkCode) =>
        notification.ImportNotificationType == ImportNotificationType.Cvedp && checkCode != null && checkCode.IsIuu();

    protected override DecisionFinderResult FindDecisionInternal(
        ImportPreNotification notification,
        CheckCode? checkCode
    )
    {
        return (notification.PartTwo?.ControlAuthority?.IuuCheckRequired == true) switch
        {
            true => notification.PartTwo?.ControlAuthority?.IuuOption switch
            {
                ControlAuthorityIuuOption.Iuuok => new DecisionFinderResult(
                    DecisionCode.C07,
                    checkCode,
                    "IUU Compliant"
                ),
                ControlAuthorityIuuOption.IUUNotCompliant => new DecisionFinderResult(
                    DecisionCode.X00,
                    checkCode,
                    "IUU Not compliant - Contact Port Health Authority (imports) or Marine Management Organisation (landings)."
                ),
                ControlAuthorityIuuOption.Iuuna => new DecisionFinderResult(
                    DecisionCode.C08,
                    checkCode,
                    "IUU Not applicable"
                ),
                null => new DecisionFinderResult(DecisionCode.X00, checkCode, "IUU Awaiting decision"),
                _ => new DecisionFinderResult(
                    DecisionCode.X00,
                    checkCode,
                    "IUU Data error",
                    DecisionInternalFurtherDetail.E95
                ),
            },
            false => new DecisionFinderResult(
                DecisionCode.X00,
                checkCode,
                "IUU Data error",
                DecisionInternalFurtherDetail.E94
            ),
        };
    }
}
