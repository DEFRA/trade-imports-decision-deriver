using AutoFixture;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;

namespace Defra.TradeImportsDecisionDeriver.TestFixtures;

public static class ClearanceRequestFixtures
{
    public static ClearanceRequest ClearanceRequestFixture()
    {
        var fixture = new Fixture();
        fixture.Customize<DateOnly>(o => o.FromFactory((DateTime dt) => DateOnly.FromDateTime(dt)));

        return fixture.Build<ClearanceRequest>().Create();
    }
}
