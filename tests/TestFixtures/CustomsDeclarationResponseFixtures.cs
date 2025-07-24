using AutoFixture;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;

namespace Defra.TradeImportsDecisionDeriver.TestFixtures;

public static class CustomsDeclarationResponseFixtures
{
    private static Fixture GetFixture()
    {
        var fixture = new Fixture();
        fixture.Customize<DateOnly>(o => o.FromFactory((DateTime dt) => DateOnly.FromDateTime(dt)));
        return fixture;
    }

    public static CustomsDeclarationResponse CustomsDeclarationResponseFixture(
        string mrn = "mrn123",
        string? documentReferencePrefix = null
    )
    {
        var fixture = GetFixture();
        var response = fixture
            .Build<CustomsDeclarationResponse>()
            .With(x => x.MovementReferenceNumber, mrn)
            .With(x => x.ClearanceRequest)
            .Create();

        int documentReferenceCount = 1;

        foreach (var commodity in response.ClearanceRequest?.Commodities!)
        {
            foreach (var document in commodity.Documents!)
            {
                document.DocumentCode = "C640";

                if (document.DocumentReference != null && documentReferencePrefix is not null)
                    document.DocumentReference.Value = $"{documentReferencePrefix}-{documentReferenceCount}";

                documentReferenceCount++;
            }
        }

        return response;
    }

    public static CustomsDeclarationResponse CustomsDeclarationResponseSimpleStaticFixture(string mrn = "mrn123")
    {
        return new CustomsDeclarationResponse(
            mrn,
            new ClearanceRequest()
            {
                ExternalVersion = 2,
                Commodities =
                [
                    new Commodity()
                    {
                        ItemNumber = 1,
                        Checks = [new CommodityCheck() { CheckCode = "H218", DepartmentCode = "HMI" }],
                        Documents =
                        [
                            new ImportDocument()
                            {
                                DocumentCode = "N002",
                                DocumentReference = new ImportDocumentReference("GBCHD2025.6244952"),
                            },
                        ],
                    },
                ],
            },
            null,
            null,
            null,
            DateTime.Now,
            DateTime.Now
        );
    }
}
