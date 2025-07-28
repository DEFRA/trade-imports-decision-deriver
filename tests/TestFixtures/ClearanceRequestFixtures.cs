using AutoFixture;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDecisionDeriver.Deriver.Entities;

namespace Defra.TradeImportsDecisionDeriver.TestFixtures;

public static class ClearanceRequestFixtures
{
    public static ResourceEvent<CustomsDeclarationEntity> ClearanceRequestCreatedFixture()
    {
        var fixture = new Fixture();
        fixture.Customize<DateOnly>(o => o.FromFactory((DateTime dt) => DateOnly.FromDateTime(dt)));

        return fixture
            .Build<ResourceEvent<CustomsDeclarationEntity>>()
            .With(x => x.Operation, ResourceEventOperations.Created)
            .With(x => x.SubResourceType, ResourceEventSubResourceTypes.ClearanceRequest)
            .Create();
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
