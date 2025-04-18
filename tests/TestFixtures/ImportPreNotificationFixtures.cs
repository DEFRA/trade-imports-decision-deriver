using AutoFixture;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

namespace Defra.TradeImportsDecisionDeriver.TestFixtures;

public static class ImportPreNotificationFixtures
{
    public static ResourceEvent<object> ImportPreNotificationCreatedFixture()
    {
        var fixture = new Fixture();
        fixture.Customize<DateOnly>(o => o.FromFactory((DateTime dt) => DateOnly.FromDateTime(dt)));

        return fixture
            .Build<ResourceEvent<object>>()
            .With(i => i.Operation, "Created")
            .With(i => i.ResourceType, ResourceEventResourceTypes.ImportPreNotification)
            .With(i => i.ResourceId, "CHEDP.GB.2025.1234567")
            .With(i => i.Resource, ImportPreNotificationFixture("CHEDP.GB.2025.1234567"))
            .Create();
    }

    public static ImportPreNotification ImportPreNotificationFixture(string chedId)
    {
        var fixture = new Fixture();
        fixture.Customize<DateOnly>(o => o.FromFactory((DateTime dt) => DateOnly.FromDateTime(dt)));

        return fixture.Build<ImportPreNotification>().With(i => i.ReferenceNumber, chedId).Create();
    }
}
