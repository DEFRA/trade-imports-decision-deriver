using Defra.TradeImportsDecisionDeriver.Deriver.Matching;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

public class DecisionContext(
    List<DecisionImportPreNotification> notifications,
    List<ClearanceRequestWrapper> clearanceRequests
)
{
    public List<DecisionImportPreNotification> Notifications { get; } = notifications;
    public List<ClearanceRequestWrapper> ClearanceRequests { get; } = clearanceRequests;

    public void LogVersions(ILogger logger)
    {
        logger.LogInformation(
            "Notifications versions: {Versions}",
            string.Join(",", Notifications.Select(x => x.GetVersion()))
        );
        logger.LogInformation(
            "ClearanceRequest versions: {Versions}",
            string.Join(",", ClearanceRequests.Select(x => x.GetVersion()))
        );
    }
}
