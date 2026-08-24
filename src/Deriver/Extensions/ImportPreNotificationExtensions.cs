using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Trade.Gateway.Api.Contract.Certificate;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

public static class ImportPreNotificationExtensions
{
    public static string GetVersion(this ImportPreNotification notification)
    {
        return $"{notification.ReferenceNumber}_{notification.Status}_{notification.UpdatedSource.TrimMicroseconds():o}";
    }
}

public static class TracesChedExtensions
{
    public static string GetVersion(this DefraUNVTDCHEDProfile certificate)
    {
        var lastUpdated = certificate.ExchangedDocument.IncludedNote?.FirstOrDefault(x => x.Subject == "LAST_UPDATED");
        return $"{certificate.ExchangedDocument.Identifier}_{certificate.ExchangedDocument.NotificationStatusCode}_{lastUpdated?.CreationDateTime}";
    }
}
