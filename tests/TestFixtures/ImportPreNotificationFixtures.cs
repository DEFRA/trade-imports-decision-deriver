using AutoFixture;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

namespace Defra.TradeImportsDecisionDeriver.TestFixtures;

public static class ImportPreNotificationFixtures
{
    public static ResourceEvent<ImportPreNotification> ImportPreNotificationCreatedFixture()
    {
        var fixture = new Fixture();
        fixture.Customize<DateOnly>(o => o.FromFactory((DateTime dt) => DateOnly.FromDateTime(dt)));

        return fixture
            .Build<ResourceEvent<ImportPreNotification>>()
            .With(i => i.Operation, "Created")
            .With(i => i.ResourceType, ResourceTypes.ImportNotification)
            .With(i => i.ResourceId, "CHEDP.GB.2025.1234567")
            .Create();
    }
}
