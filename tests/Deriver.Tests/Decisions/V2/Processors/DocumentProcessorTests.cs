////using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
////using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
////using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2;
////using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.ChedDecisionResolvers;
////using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.Processors;
////using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
////using Microsoft.Extensions.Logging;
////using NSubstitute;

////namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions.V2.Processors;

////public class CheckProcessorTests
////{
////    private readonly IDocumentProcessor _documentProcessor;
////    private readonly CheckProcessor _sut;

////    public CheckProcessorTests()
////    {
////        _documentProcessor = Substitute.For<IDocumentProcessor>();
////        _sut = new CheckProcessor(_documentProcessor);
////    }

////    [Fact]
////    public void Process_WhenValidDocumentsExist_ProcessesEachDocument()
////    {
////        // Arrange
////        var document1 = new ImportDocument
////        {
////            DocumentCode = "DOC1",
////            DocumentReference = new ImportDocumentReference("REF1")
////        };

////        var document2 = new ImportDocument
////        {
////            DocumentCode = "DOC1",
////            DocumentReference = new DocumentReference { Value = "REF2" }
////        };

////        var commodity = new Commodity
////        {
////            ItemNumber = new ItemNumber { Value = 10 },
////            Documents = new[] { document1, document2 }
////        };

////        var check = new CommodityCheck { CheckCode = "A1" };

////        var expectedResult1 = Substitute.For<CheckDecisionResult>();
////        var expectedResult2 = Substitute.For<CheckDecisionResult>();

////        _documentProcessor
////            .Process(Arg.Any<DecisionContextV2>(), Arg.Any<CustomsDeclarationWrapper>(),
////                commodity, check, document1)
////            .Returns(expectedResult1);

////        _documentProcessor
////            .Process(Arg.Any<DecisionContextV2>(), Arg.Any<CustomsDeclarationWrapper>(),
////                commodity, check, document2)
////            .Returns(expectedResult2);

////        // Act
////        var results = _sut.Process(
////            new DecisionContextV2(),
////            new CustomsDeclarationWrapper(),
////            commodity,
////            check
////        );

////        // Assert
////        results.Should().HaveCount(2);
////        results.Should().ContainInOrder(expectedResult1, expectedResult2);

////        _documentProcessor.Received(2)
////            .Process(Arg.Any<DecisionContextV2>(), Arg.Any<CustomsDeclarationWrapper>(),
////                commodity, check, Arg.Any<ImportDocument>());
////    }

////    [Fact]
////    public void Process_WhenDocumentsAreInvalid_DoesNotCallDocumentProcessor()
////    {
////        // Arrange
////        var commodity = new Commodity
////        {
////            ItemNumber = new ItemNumber { Value = 1 },
////            Documents = new[]
////            {
////                new ImportDocument { DocumentCode = "INVALID" }
////            }
////        };

////        var check = new CommodityCheck { CheckCode = "A1" };

////        // Act
////        var results = _sut.Process(
////            new DecisionContextV2(),
////            new CustomsDeclarationWrapper(),
////            commodity,
////            check
////        );

////        // Assert
////        results.Should().HaveCount(1);
////        results.Single().DecisionCode.Should().Be(DecisionCode.X00);

////        _documentProcessor.DidNotReceiveWithAnyArgs()
////            .Process(default!, default!, default!, default!, default!);
////    }

////    [Fact]
////    public void Process_WhenNoValidDocumentsExist_ReturnsOrphanResult()
////    {
////        // Arrange
////        var commodity = new Commodity
////        {
////            ItemNumber = new ItemNumber { Value = 3 },
////            Documents = null
////        };

////        var check = new CommodityCheck { CheckCode = "A1" };

////        // Act
////        var results = _sut.Process(
////            new DecisionContextV2(),
////            new CustomsDeclarationWrapper { MovementReferenceNumber = "MRN1" },
////            commodity,
////            check
////        );

////        // Assert
////        results.Should().HaveCount(1);

////        var result = results.Single();
////        result.DecisionCode.Should().Be(DecisionCode.X00);
////        result.InternalFurtherDetail.Should().Be(DecisionInternalFurtherDetail.E83);
////        result.CheckCode.Should().Be("A1");
////    }

////    [Fact]
////    public void Process_WhenCheckIsH220_AndH219Exists_ReturnsE82()
////    {
////        // Arrange
////        var commodity = new Commodity
////        {
////            ItemNumber = new ItemNumber { Value = 5 },
////            Checks = new[]
////            {
////                new CommodityCheck { CheckCode = "H219" }
////            }
////        };

////        var check = new CommodityCheck { CheckCode = "H220" };

////        // Act
////        var results = _sut.Process(
////            new DecisionContextV2(),
////            new CustomsDeclarationWrapper(),
////            commodity,
////            check
////        );

////        // Assert
////        results.Single()
////            .InternalFurtherDetail
////            .Should()
////            .Be(DecisionInternalFurtherDetail.E82);
////    }

////    [Fact]
////    public void Process_WhenCheckIsH220_AndH219DoesNotExist_ReturnsE87()
////    {
////        // Arrange
////        var commodity = new Commodity
////        {
////            ItemNumber = 5,
////            Checks = new[]
////            {
////                new CommodityCheck { CheckCode = "OTHER" }
////            }
////        };

////        var check = new CommodityCheck { CheckCode = "H220" };

////        // Act
////        var results = _sut.Process(
////            new DecisionContextV2(),
////            new CustomsDeclarationWrapper(),
////            commodity,
////            check
////        );

////        // Assert
////        results.Single()
////            .InternalFurtherDetail
////            .Should()
////            .Be(DecisionInternalFurtherDetail.E87);
////    }
////}

////}

////public class DocumentProcessorTests
////{
////    private readonly IDecisionRulesEngineFactory _factory;
////    private readonly IImportPreNotificationDecisionResolver _resolver;
////    private readonly ILogger<DocumentProcessor> _logger;

////    private readonly DocumentProcessor _sut;

////    public DocumentProcessorTests()
////    {
////        _factory = Substitute.For<IDecisionRulesEngineFactory>();
////        _resolver = Substitute.For<IImportPreNotificationDecisionResolver>();
////        _logger = Substitute.For<ILogger<DocumentProcessor>>();

////        _sut = new DocumentProcessor(_factory, _logger);
////    }

////    [Fact]
////    public void Process_WhenMatchingNotificationExists_ReturnsExpectedDecisionResult()
////    {
////        // Arrange
////        var check = new CommodityCheck { CheckCode = "A1" };

////        var document = new ImportDocument
////        {
////            DocumentCode = "DOC", DocumentReference = new ImportDocumentReference("REF123")
////        };

////        var commodity = new Commodity { ItemNumber = 5 };

////        var clearanceRequest = new CustomsDeclarationWrapper("MRN123", new CustomsDeclaration());

////        var notification = new DecisionImportPreNotification { Id = "NOTIF-1" };

////        var context = new DecisionContextV2([notification], []);

////        var resolverResult = new DecisionResolutionResult(
////            DecisionCode.C02,
////            DecisionInternalFurtherDetail.E70
////        );

////        _factory
////            .Get(Arg.Any<string>())
////            .Returns(_resolver);

////        _resolver
////            .Resolve(Arg.Any<DecisionResolutionContext>())
////            .Returns(resolverResult);

////        // Act
////        var result = _sut.Process(
////            context,
////            clearanceRequest,
////            commodity,
////            check,
////            document
////        );

////        // Assert
////        Assert.Equal("MRN123", result.Mrn);
////        Assert.Equal(5, result.ItemNumber);
////        Assert.Equal("REF123", result.DocumentReference);
////        Assert.Equal("DOC", result.DocumentCode);
////        Assert.Equal("A1", result.CheckCode);
////        Assert.Equal(DecisionCode.C02, result.DecisionCode);
////        Assert.Equal(DecisionInternalFurtherDetail.E70, result.InternalDecisionCode);

////        _factory.Received(1).Get(Arg.Any<string>());
////        _resolver.Received(1).Resolve(Arg.Any<DecisionResolutionContext>());
////    }

////    [Fact]
////    public void Process_WhenNoMatchingNotificationExists_PassesNullNotificationToResolver()
////    {
////        // Arrange
////        var context = new DecisionContextV2([], []);

////        var check = new CommodityCheck { CheckCode = "A1" };
////        var document = new ImportDocument { DocumentCode = "DOC" };

////        _factory
////            .Get(Arg.Any<string>())
////            .Returns(_resolver);

////        _resolver
////            .Resolve(Arg.Is<DecisionResolutionContext>(c => c.Notification == null))
////            .Returns(new DecisionResolutionResult(DecisionCode.N01, null));

////        // Act
////        var result = _sut.Process(
////            context,
////            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
////            new Commodity { ItemNumber = 1 },
////            check,
////            document
////        );

////        // Assert
////        Assert.Equal(DecisionCode.N01, result.DecisionCode);

////        _resolver.Received(1)
////            .Resolve(Arg.Is<DecisionResolutionContext>(c => c.Notification == null));
////    }

////    [Fact]
////    public void Process_PassesCorrectDataIntoResolverContext()
////    {
////        // Arrange
////        DecisionResolutionContext capturedContext = null!;

////        _factory
////            .Get(Arg.Any<string>())
////            .Returns(_resolver);

////        _resolver
////            .Resolve(Arg.Do<DecisionResolutionContext>(c => capturedContext = c))
////            .Returns(new DecisionResolutionResult(DecisionCode.C02, null));

////        var context = new DecisionContextV2([], []);

////        var commodity = new Commodity
////        {
////            ItemNumber = 99
////        };

////        // Act
////        _sut.Process(
////            context,
////            new CustomsDeclarationWrapper("mrn", new CustomsDeclaration()),
////            commodity,
////            new CommodityCheck { CheckCode = "B2" },
////            new ImportDocument { DocumentCode = "DOC" }
////        );

////        // Assert
////        Assert.NotNull(capturedContext);
////        Assert.Equal("B2", capturedContext.CheckCode.Value);
////        Assert.Same(_logger, capturedContext.Logger);
////        Assert.Same(commodity, capturedContext.Commodity);
////    }
////}
