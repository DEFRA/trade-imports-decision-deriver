using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;

namespace Defra.TradeImportsDecisionDeriver.TestFixtures;

public sealed class CustomsDeclarationsWrapperBuilder
{
    private string _movementReferenceNumber = "MRN000000000";
    private readonly DateTime _messageSentAt = DateTime.UtcNow;
    private readonly List<Commodity> _commodities = new();

    public static CustomsDeclarationsWrapperBuilder Create() => new();

    public CustomsDeclarationsWrapperBuilder WithMrn(string mrn)
    {
        _movementReferenceNumber = mrn ?? throw new ArgumentNullException(nameof(mrn));
        return this;
    }

    public CustomsDeclarationsWrapperBuilder AddCommodity(Action<CommodityBuilder> configure)
    {
        var builder = new CommodityBuilder();
        configure(builder);
        _commodities.Add(builder.Build());
        return this;
    }

    public CustomsDeclarationWrapper Build()
    {
        var clearanceRequest = new ClearanceRequest
        {
            MessageSentAt = _messageSentAt,
            Commodities = _commodities.Count == 0 ? Array.Empty<Commodity>() : _commodities.ToArray(),
        };

        var cd = new CustomsDeclaration() { ClearanceRequest = clearanceRequest };

        return new CustomsDeclarationWrapper(_movementReferenceNumber, cd);
    }

    public sealed class CommodityBuilder
    {
        private int? _itemNumber;
        private readonly List<ImportDocument> _documents = new();
        private readonly List<CommodityCheck> _checks = new();

        public CommodityBuilder WithItemNumber(int itemNumber)
        {
            _itemNumber = itemNumber;
            return this;
        }

        public CommodityBuilder AddDocument(string documentCode, string documentReferenceValue)
        {
            var doc = new ImportDocument
            {
                DocumentCode = documentCode,
                DocumentReference = new ImportDocumentReference(documentReferenceValue),
            };
            _documents.Add(doc);
            return this;
        }

        // New: fluent document builder overload
        ////public CommodityBuilder AddDocument(Action<ImportDocumentBuilder> configure)
        ////{
        ////	var builder = ImportDocumentBuilder.Create();
        ////	configure(builder);
        ////	_documents.Add(builder.Build());
        ////	return this;
        ////}

        public CommodityBuilder AddCheck(string checkCode)
        {
            _checks.Add(new CommodityCheck { CheckCode = checkCode });
            return this;
        }

        public CommodityBuilder AddChecks(params string[] checkCodes)
        {
            foreach (var checkCode in checkCodes)
            {
                AddCheck(checkCode);
            }
            return this;
        }

        public Commodity Build()
        {
            return new Commodity
            {
                ItemNumber = _itemNumber,
                Documents = _documents.Count == 0 ? Array.Empty<ImportDocument>() : _documents.ToArray(),
                Checks = _checks.Count == 0 ? Array.Empty<CommodityCheck>() : _checks.ToArray(),
            };
        }
    }
}
