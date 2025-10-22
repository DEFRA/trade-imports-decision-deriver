using Defra.TradeImportsDataApi.Domain.Ipaffs;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

public static class ImportPreNotificationExtensions
{
    public static string GetVersion(this ImportPreNotification notification)
    {
        return $"{notification.ReferenceNumber}_{notification.Status}_{notification.UpdatedSource.TrimMicroseconds():o}";
    }
}
