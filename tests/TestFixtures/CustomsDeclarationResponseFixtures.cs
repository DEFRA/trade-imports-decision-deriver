using AutoFixture;
using Defra.TradeImportsDataApi.Api.Client;

namespace Defra.TradeImportsDecisionDeriver.TestFixtures;

public static class CustomsDeclarationResponseFixtures
{
    public static CustomsDeclarationResponse CustomsDeclarationResponseFixture(string mrn = "mrn123")
    {
        var fixture = new Fixture();
        fixture.Customize<DateOnly>(o => o.FromFactory((DateTime dt) => DateOnly.FromDateTime(dt)));

        var response = fixture.Build<CustomsDeclarationResponse>().With(x => x.MovementReferenceNumber, mrn).Create();

        foreach (var commodity in response.ClearanceRequest?.Commodities!)
        {
            foreach (var document in commodity.Documents!)
            {
                document.DocumentCode = "C640";
            }
        }

        return response;
    }
}
