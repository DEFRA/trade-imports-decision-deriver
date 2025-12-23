namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine.DecisionRules;

public sealed class ChedppDecisionRule : IDecisionRule
{
    public DecisionResolutionResult Execute(DecisionResolutionContext context, DecisionRuleDelegate next)
    {
        return context.Notification.Status switch
        {
            ImportNotificationStatus.Submitted or ImportNotificationStatus.InProgress => new DecisionResolutionResult(
                DecisionCode.H02
            ),
            ImportNotificationStatus.Validated or ImportNotificationStatus.Rejected => context.CheckCode.Value switch
            {
                "H218" or "H220" => ProcessHmi(context.Notification),
                "H219" => ProcessPhsi(context.Notification),
                _ => new DecisionResolutionResult(DecisionCode.X00, DecisionInternalFurtherDetail.E99),
            },
            ImportNotificationStatus.PartiallyRejected => new DecisionResolutionResult(
                DecisionCode.H01,
                DecisionInternalFurtherDetail.E74
            ),
            _ => new DecisionResolutionResult(DecisionCode.X00, DecisionInternalFurtherDetail.E99),
        };
    }

    private static DecisionResolutionResult ProcessHmi(DecisionImportPreNotification notification)
    {
        var hmiCheck = notification.CommodityChecks.FirstOrDefault(x => x.Type == "HMI");

        if (hmiCheck is null)
        {
            return new DecisionResolutionResult(DecisionCode.H01, DecisionInternalFurtherDetail.E86);
        }

        return GetDecisionFromStatus(hmiCheck.Status);
    }

    private static DecisionResolutionResult ProcessPhsi(DecisionImportPreNotification notification)
    {
        var documentCheck = notification.CommodityChecks.FirstOrDefault(x => x.Type == "PHSI_DOCUMENT");
        var physicalCheck = notification.CommodityChecks.FirstOrDefault(x => x.Type == "PHSI_PHYSICAL");
        var identifyCheck = notification.CommodityChecks.FirstOrDefault(x => x.Type == "PHSI_IDENTITY");

        if (documentCheck is null || physicalCheck is null || identifyCheck is null)
        {
            return new DecisionResolutionResult(DecisionCode.H01, DecisionInternalFurtherDetail.E85);
        }

        var decisions = new List<DecisionResolutionResult>
        {
            GetDecisionFromStatus(documentCheck.Status),
            GetDecisionFromStatus(physicalCheck.Status),
            GetDecisionFromStatus(identifyCheck.Status),
        };

        return decisions.OrderByDescending(x => x.Code).First();
    }

    private static DecisionResolutionResult GetDecisionFromStatus(string status)
    {
        return status switch
        {
            "To do" or "Hold" => new DecisionResolutionResult(DecisionCode.H01),
            "To be inspected" => new DecisionResolutionResult(DecisionCode.H02),
            "Compliant" or "Auto cleared" => new DecisionResolutionResult(DecisionCode.C03),
            "Non compliant" => new DecisionResolutionResult(DecisionCode.N01),
            "Not inspected" => new DecisionResolutionResult(DecisionCode.C02),
            _ => new DecisionResolutionResult(DecisionCode.X00, DecisionInternalFurtherDetail.E99),
        };
    }
}
