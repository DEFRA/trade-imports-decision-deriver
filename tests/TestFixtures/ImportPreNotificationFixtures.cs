using AutoFixture;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

namespace Defra.TradeImportsDecisionDeriver.TestFixtures;

public static class ImportPreNotificationFixtures
{
    private const string Ched = "CHEDP.GB.2025.1234567";

    public static ResourceEvent<object> ImportPreNotificationCreatedFixture()
    {
        var fixture = new Fixture();
        fixture.Customize<DateOnly>(o => o.FromFactory((DateTime dt) => DateOnly.FromDateTime(dt)));

        return fixture
            .Build<ResourceEvent<object>>()
            .With(i => i.Operation, "Created")
            .With(i => i.ResourceType, ResourceEventResourceTypes.ImportPreNotification)
            .With(i => i.ResourceId, Ched)
            .With(i => i.Resource, ImportPreNotificationFixture(Ched))
            .Create();
    }

    public static ImportPreNotification ImportPreNotificationFixture(
        string chedId,
        string? status = ImportNotificationStatus.InProgress
    )
    {
        var fixture = new Fixture();
        fixture.Customize<DateOnly>(o => o.FromFactory((DateTime dt) => DateOnly.FromDateTime(dt)));

        var uniqueIds = fixture.CreateMany<int>(3).ToList();

        var commodityComplements = uniqueIds
            .Select(id =>
                fixture
                    .Build<CommodityComplement>()
                    .With(x => x.UniqueComplementId, id.ToString)
                    .With(x => x.ComplementId, id)
                    .Create()
            )
            .ToArray();

        var commodityParameterSets = uniqueIds
            .Select(id =>
                fixture
                    .Build<ComplementParameterSet>()
                    .With(x => x.UniqueComplementId, id.ToString)
                    .With(x => x.ComplementId, id)
                    .Create()
            )
            .ToArray();

        var commodityResults = uniqueIds
            .Select(id => fixture.Build<CommodityRiskResult>().With(x => x.UniqueId, id.ToString).Create())
            .ToArray();
        var riskAssessment = fixture
            .Build<RiskAssessmentResult>()
            .With(x => x.CommodityResults, commodityResults)
            .Create();

        var commodities = fixture
            .Build<Commodities>()
            .With(x => x.CommodityComplements, commodityComplements)
            .With(x => x.ComplementParameterSets, commodityParameterSets)
            .Create();

        var partOne = fixture.Build<PartOne>().With(x => x.Commodities, commodities).Create();

        return fixture
            .Build<ImportPreNotification>()
            .With(x => x.ReferenceNumber, chedId)
            .With(x => x.PartOne, partOne)
            .With(x => x.RiskAssessment, riskAssessment)
            .With(x => x.Status, status)
            .Create();
    }

    public static ImportPreNotificationResponse ImportPreNotificationResponseFixture()
    {
        var fixture = new Fixture();
        fixture.Customize<DateOnly>(o => o.FromFactory((DateTime dt) => DateOnly.FromDateTime(dt)));

        return fixture
            .Build<ImportPreNotificationResponse>()
            .With(i => i.ImportPreNotification, ImportPreNotificationFixture(Ched))
            .Create();
    }
}
