using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.TestFixtures;

// ReSharper disable InconsistentNaming

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions;

public class ClearanceDecisionBuilderTests
{
    private readonly VerifySettings _settings;

    public ClearanceDecisionBuilderTests()
    {
        _settings = new VerifySettings();
        _settings.IgnoreMember<ClearanceDecision>(x => x.Created);
        _settings.DontScrubDateTimes();
        _settings.DontScrubGuids();
    }

    [Fact]
    public async Task BuildClearanceDecision_WithNoReasons()
    {
        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture();
        customsDeclaration.ClearanceRequest!.ExternalCorrelationId = "correlationId";
        customsDeclaration.ClearanceRequest!.ExternalVersion = 22;
        var decisionResult = new DecisionResult();
        for (var i = 0; i < (customsDeclaration.ClearanceRequest?.Commodities!).Length; i++)
        {
            var commodity = (customsDeclaration.ClearanceRequest?.Commodities!)[i];
            commodity.ItemNumber = i + 1;
            commodity.Checks = commodity.Checks!.Take(1).ToArray();
            commodity.Checks[0].CheckCode = "9115";

            var documentReferenceCount = 1;
            foreach (var document in commodity.Documents!)
            {
                document.DocumentCode = "9115";
                document.DocumentReference!.Value = $"docref_{commodity.ItemNumber}_{documentReferenceCount}";
                decisionResult.AddDecision(
                    customsDeclaration.MovementReferenceNumber,
                    commodity.ItemNumber!.Value!,
                    document.DocumentReference!.Value,
                    document.DocumentCode,
                    commodity.Checks[0].CheckCode,
                    DecisionCode.C03
                );

                documentReferenceCount++;
            }
        }

        var clearanceDecision = decisionResult.BuildClearanceDecision(
            customsDeclaration.MovementReferenceNumber,
            new CustomsDeclaration { ClearanceRequest = customsDeclaration.ClearanceRequest },
            new TestCorrelationIdGenerator("correlationId")
        );

        await Verify(clearanceDecision, _settings).UseMethodName(nameof(BuildClearanceDecision_WithNoReasons));
    }

    [Fact]
    public async Task BuildClearanceDecision_WithReasons()
    {
        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture(
            documentReferencePrefix: "fixed"
        );
        customsDeclaration.ClearanceRequest!.ExternalCorrelationId = "correlationId";
        customsDeclaration.ClearanceRequest!.ExternalVersion = 22;
        var decisionResult = new DecisionResult();
        for (var i = 0; i < (customsDeclaration.ClearanceRequest?.Commodities!).Length; i++)
        {
            var commodity = (customsDeclaration.ClearanceRequest?.Commodities!)[i];
            commodity.ItemNumber = i + 1;
            commodity.Checks = commodity.Checks!.Take(1).ToArray();
            commodity.Checks[0].CheckCode = "9115";
            foreach (var document in commodity.Documents!)
            {
                document.DocumentCode = "9115";
                if (i < 1)
                {
                    decisionResult.AddDecision(
                        customsDeclaration.MovementReferenceNumber,
                        commodity.ItemNumber!.Value!,
                        document.DocumentReference!.Value,
                        document.DocumentCode,
                        commodity.Checks[0].CheckCode,
                        DecisionCode.X00,
                        internalDecisionCode: DecisionInternalFurtherDetail.E71
                    );
                }
                else
                {
                    decisionResult.AddDecision(
                        customsDeclaration.MovementReferenceNumber,
                        commodity.ItemNumber!.Value!,
                        document.DocumentReference!.Value,
                        document.DocumentCode,
                        commodity.Checks[0].CheckCode,
                        DecisionCode.X00
                    );
                }
            }
        }

        var clearanceDecision = decisionResult.BuildClearanceDecision(
            customsDeclaration.MovementReferenceNumber,
            new CustomsDeclaration { ClearanceRequest = customsDeclaration.ClearanceRequest },
            new TestCorrelationIdGenerator("correlationId")
        );

        await Verify(clearanceDecision, _settings).UseMethodName(nameof(BuildClearanceDecision_WithReasons));
    }

    [Fact]
    public async Task BuildClearanceDecision_WithNoReasons_AndPreviousDecision()
    {
        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture();
        customsDeclaration.ClearanceRequest!.ExternalCorrelationId = "correlationId";
        customsDeclaration.ClearanceDecision!.CorrelationId = "correlationId";
        customsDeclaration.ClearanceRequest!.ExternalVersion = 22;
        customsDeclaration.ClearanceDecision!.DecisionNumber = 4;
        var decisionResult = new DecisionResult();
        for (var i = 0; i < (customsDeclaration.ClearanceRequest?.Commodities!).Length; i++)
        {
            var commodity = (customsDeclaration.ClearanceRequest?.Commodities!)[i];
            commodity.ItemNumber = i + 1;
            commodity.Checks = commodity.Checks!.Take(1).ToArray();
            commodity.Checks[0].CheckCode = "9115";

            var documentReferenceCount = 1;
            foreach (var document in commodity.Documents!)
            {
                document.DocumentCode = "9115";
                document.DocumentReference!.Value = $"docref_{commodity.ItemNumber}_{documentReferenceCount}";
                decisionResult.AddDecision(
                    customsDeclaration.MovementReferenceNumber,
                    commodity.ItemNumber!.Value!,
                    document.DocumentReference!.Value,
                    document.DocumentCode,
                    commodity.Checks[0].CheckCode,
                    DecisionCode.C03
                );

                documentReferenceCount++;
            }
        }

        var clearanceDecision = decisionResult.BuildClearanceDecision(
            customsDeclaration.MovementReferenceNumber,
            new CustomsDeclaration
            {
                ClearanceRequest = customsDeclaration.ClearanceRequest,
                ClearanceDecision = customsDeclaration.ClearanceDecision,
            },
            new TestCorrelationIdGenerator("correlationId")
        );

        await Verify(clearanceDecision, _settings)
            .UseMethodName(nameof(BuildClearanceDecision_WithNoReasons_AndPreviousDecision));
    }

    [Fact]
    public async Task BuildClearanceDecision_WithNoReasons_ForE83()
    {
        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture();
        customsDeclaration.ClearanceRequest!.ExternalCorrelationId = "correlationId";
        customsDeclaration.ClearanceDecision!.CorrelationId = "correlationId";
        customsDeclaration.ClearanceRequest!.ExternalVersion = 22;
        customsDeclaration.ClearanceDecision!.DecisionNumber = 4;
        var decisionResult = new DecisionResult();
        for (var i = 0; i < (customsDeclaration.ClearanceRequest?.Commodities!).Length; i++)
        {
            var commodity = (customsDeclaration.ClearanceRequest?.Commodities!)[i];
            commodity.ItemNumber = i + 1;
            commodity.Checks = commodity.Checks!.Take(1).ToArray();
            commodity.Checks[0].CheckCode = "9115";

            var documentReferenceCount = 1;
            foreach (var document in commodity.Documents!)
            {
                document.DocumentCode = "9115";
                document.DocumentReference!.Value = $"docref_{commodity.ItemNumber}_{documentReferenceCount}";
                decisionResult.AddDecision(
                    customsDeclaration.MovementReferenceNumber,
                    commodity.ItemNumber!.Value!,
                    document.DocumentReference!.Value,
                    document.DocumentCode,
                    commodity.Checks[0].CheckCode,
                    DecisionCode.X00,
                    internalDecisionCode: DecisionInternalFurtherDetail.E83
                );

                documentReferenceCount++;
            }
        }

        var clearanceDecision = decisionResult.BuildClearanceDecision(
            customsDeclaration.MovementReferenceNumber,
            new CustomsDeclaration
            {
                ClearanceRequest = customsDeclaration.ClearanceRequest,
                ClearanceDecision = customsDeclaration.ClearanceDecision,
            },
            new TestCorrelationIdGenerator("correlationId")
        );

        await Verify(clearanceDecision, _settings).UseMethodName(nameof(BuildClearanceDecision_WithNoReasons_ForE83));
    }
}
