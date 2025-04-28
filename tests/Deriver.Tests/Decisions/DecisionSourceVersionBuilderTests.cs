using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Decisions;

public class DecisionSourceVersionBuilderTests
{
    [Fact]
    public void WhenNoNotificationsExists_ThenSourceVersionShouldBeJustCRVersion()
    {
        // Arrange
        var decisionResult = new DecisionResult();

        // Act
        var sourceVersion = decisionResult.BuildDecisionSourceVersion(6);

        // Assert
        sourceVersion.Should().Be("CR-VERSION-6");
    }

    [Fact]
    public void WhenSingleNotificationsExists_ThenSourceVersionShouldBeNotificationAndCRVersion()
    {
        // Arrange
        var decisionResult = new DecisionResult();
        decisionResult.AddDecision(
            "mrn",
            1,
            "documentRef",
            "checkCode",
            DecisionCode.C03,
            new DecisionImportPreNotification()
            {
                Id = "TestId",
                UpdatedSource = new DateTime(2025, 2, 2, 8, 6, 23, DateTimeKind.Local),
            }
        );

        // Act
        var sourceVersion = decisionResult.BuildDecisionSourceVersion(6);

        // Assert
        sourceVersion.Should().Be("TestId:020225080623:CR-VERSION-6");
    }

    [Fact]
    public void WheMultipleNotificationsExists_ThenSourceVersionShouldBeNotificationsAndCRVersion()
    {
        // Arrange
        var decisionResult = new DecisionResult();
        decisionResult.AddDecision(
            "mrn",
            1,
            "documentRef",
            "checkCode",
            DecisionCode.C03,
            new DecisionImportPreNotification()
            {
                Id = "TestId1",
                UpdatedSource = new DateTime(2025, 2, 2, 8, 6, 23, DateTimeKind.Local),
            }
        );
        decisionResult.AddDecision(
            "mrn",
            1,
            "documentRef",
            "checkCode",
            DecisionCode.C03,
            new DecisionImportPreNotification()
            {
                Id = "TestId2",
                UpdatedSource = new DateTime(2025, 4, 5, 5, 7, 42, DateTimeKind.Local),
            }
        );

        // Act
        var sourceVersion = decisionResult.BuildDecisionSourceVersion(6);

        // Assert
        sourceVersion.Should().Be("TestId1:020225080623-TestId2:050425050742:CR-VERSION-6");
    }
}
