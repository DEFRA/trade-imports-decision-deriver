namespace Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

public static class DecisionSourceVersionBuilder
{
    public static string BuildDecisionSourceVersion(this DecisionResult decisionResult, int? clearanceRequestVersion)
    {
        var notifications = decisionResult
            .Decisions.Where(x => x.PreNotification is not null)
            .Select(x => $"{x.PreNotification?.Id}:{x.PreNotification?.UpdatedSource:ddMMyyhhmmss}")
            .ToList();

        if (notifications.Count != 0)
        {
            return $"{string.Join('-', notifications)}:CR-VERSION-{clearanceRequestVersion}";
        }

        return $"CR-VERSION-{clearanceRequestVersion}";
    }
}
