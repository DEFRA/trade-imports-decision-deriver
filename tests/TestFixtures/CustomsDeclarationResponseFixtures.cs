using AutoFixture;
using Defra.TradeImportsDataApi.Api.Client;

namespace Defra.TradeImportsDecisionDeriver.TestFixtures;

public static class CustomsDeclarationResponseFixtures
{
    public static CustomsDeclarationResponse CustomsDeclarationResponseFixture()
    {
        var fixture = new Fixture();
        fixture.Customize<DateOnly>(o => o.FromFactory((DateTime dt) => DateOnly.FromDateTime(dt)));

        return fixture.Build<CustomsDeclarationResponse>().Create();
    }
}
