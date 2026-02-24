using System.Runtime.CompilerServices;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;

public sealed class ChedppDecisionRule : IDecisionRule
{
    public DecisionEngineResult Execute(DecisionEngineContext context, DecisionRuleDelegate next)
    {
        return context.Notification.Status switch
        {
            ImportNotificationStatus.Submitted or ImportNotificationStatus.InProgress => DecisionEngineResult.H02,
            ImportNotificationStatus.Validated or ImportNotificationStatus.Rejected => context.CheckCode.Value switch
            {
                "H218" or "H220" => ProcessHmi(context.Notification),
                "H219" => ProcessPhsi(context.Notification),
                _ => DecisionEngineResult.X00E99,
            },
            ImportNotificationStatus.PartiallyRejected => DecisionEngineResult.H01E74,
            _ => DecisionEngineResult.X00E99,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DecisionEngineResult ProcessHmi(DecisionImportPreNotification notification)
    {
        var hmiCheck = notification.CommodityChecks.FirstOrDefault(x => x.Type == "HMI");

        if (hmiCheck is null)
        {
            return DecisionEngineResult.H01E86;
        }

        return GetDecisionFromStatus(hmiCheck.Status);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DecisionEngineResult ProcessPhsi(DecisionImportPreNotification notification)
    {
        var documentCheck = notification.CommodityChecks.FirstOrDefault(x => x.Type == "PHSI_DOCUMENT");
        var physicalCheck = notification.CommodityChecks.FirstOrDefault(x => x.Type == "PHSI_PHYSICAL");
        var identifyCheck = notification.CommodityChecks.FirstOrDefault(x => x.Type == "PHSI_IDENTITY");

        if (documentCheck is null || physicalCheck is null || identifyCheck is null)
        {
            return DecisionEngineResult.H01E85;
        }

        var decisions = new List<DecisionEngineResult>
        {
            GetDecisionFromStatus(documentCheck.Status),
            GetDecisionFromStatus(physicalCheck.Status),
            GetDecisionFromStatus(identifyCheck.Status),
        };

        return decisions.OrderByDescending(x => x.Code).First();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DecisionEngineResult GetDecisionFromStatus(string status)
    {
        return status switch
        {
            "To do" or "Hold" => DecisionEngineResult.H01,
            "To be inspected" => DecisionEngineResult.H02,
            "Compliant" or "Auto cleared" => DecisionEngineResult.C03,
            "Non compliant" => DecisionEngineResult.N01,
            "Not inspected" => DecisionEngineResult.C02,
            _ => DecisionEngineResult.X00E99,
        };
    }
}
