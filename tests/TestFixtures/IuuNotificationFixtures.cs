using AutoFixture;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Iuu;

namespace Defra.TradeImportsDecisionDeriver.TestFixtures;

public static class IuuNotificationFixtures
{
    private const string Ched = "CHEDP.GB.2025.1234567";

    public static ResourceEvent<IuuNotificationEvent> IuuNotificationCreatedFixture(string chedId = Ched)
    {
        var fixture = new Fixture();
        fixture.Customize<DateOnly>(o => o.FromFactory((DateTime dt) => DateOnly.FromDateTime(dt)));

        return fixture
            .Build<ResourceEvent<IuuNotificationEvent>>()
            .With(i => i.Operation, ResourceEventOperations.Created)
            .With(i => i.ResourceType, ResourceEventResourceTypes.IuuNotification)
            .With(i => i.ResourceId, chedId)
            .With(i => i.Resource, IuuNotificationEntityFixture(chedId))
            .Create();
    }

    public static IuuNotificationEvent IuuNotificationEntityFixture(string chedId)
    {
        return new IuuNotificationEvent()
        {
            Id = chedId,
            IuuNotification = new IuuNotification() { Reference = chedId, LastUpdated = DateTime.UtcNow },
        };
    }

    public static RelatedImportDeclarationsResponse RelatedImportDeclarationsResponseFixture(
        params CustomsDeclarationResponse[] customsDeclarations
    )
    {
        return new RelatedImportDeclarationsResponse(customsDeclarations, [], [], [], [], [], []);
    }
}
