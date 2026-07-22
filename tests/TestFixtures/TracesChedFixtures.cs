using AutoFixture;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Trade.Gateway.Api.Contract.Certificate;

namespace Defra.TradeImportsDecisionDeriver.TestFixtures;

public static class TracesChedFixtures
{
    private const string Ched = "CHEDP.GB.2025.1234567";

    public static ResourceEvent<TracesChedEvent> TracesChedCreatedFixture()
    {
        var fixture = new Fixture();
        fixture.Customize<DateOnly>(o => o.FromFactory((DateTime dt) => DateOnly.FromDateTime(dt)));

        return fixture
            .Build<ResourceEvent<TracesChedEvent>>()
            .With(i => i.Operation, "Created")
            .With(i => i.ResourceType, ResourceEventResourceTypes.TracesChed)
            .With(i => i.ResourceId, Ched)
            .With(i => i.Resource, TracesChedEntityFixture(Ched))
            .Create();
    }

    public static TracesChedEvent TracesChedEntityFixture(
        string chedId,
        string? status = ImportNotificationStatus.InProgress
    )
    {
        return new TracesChedEvent()
        {
            Id = chedId,
            Ched = new DefraUNVTDCHEDProfile()
            {
                SpecifiedConsignment = new Consignment(),
                ExchangedDocument = new ExchangedDocument() { Identifier = chedId },
            },
        };
    }
}
