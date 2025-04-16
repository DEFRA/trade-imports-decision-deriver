using AutoFixture;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;

namespace Defra.TradeImportsDecisionDeriver.TestFixtures;

public static class ClearanceRequestFixtures
{
    public static ResourceEvent<object> ClearanceRequestCreatedFixture()
    {
        var fixture = new Fixture();
        fixture.Customize<DateOnly>(o => o.FromFactory((DateTime dt) => DateOnly.FromDateTime(dt)));

        return fixture.Build<ResourceEvent<object>>().With(x => x.Operation, ResourceEventOperations.Created).Create();
    }

    public static ResourceEvent<object> ClearanceRequestUpdatedFixture()
    {
        var fixture = new Fixture();
        fixture.Customize<DateOnly>(o => o.FromFactory((DateTime dt) => DateOnly.FromDateTime(dt)));

        return fixture.Build<ResourceEvent<object>>().With(x => x.Operation, ResourceEventOperations.Updated).Create();
    }

    public static ClearanceRequest ClearanceRequestFixture()
    {
        var fixture = new Fixture();
        fixture.Customize<DateOnly>(o => o.FromFactory((DateTime dt) => DateOnly.FromDateTime(dt)));

        return fixture.Build<ClearanceRequest>().Create();
    }
}
