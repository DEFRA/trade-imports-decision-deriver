using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.TestFixtures;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Extensions;

public class ImportPreNotificationExtensionsTests
{
    [Fact]
    public void When_No_ComplementParameter_For_Complement_Then_Should_Not_Throw()
    {
        var notification = ImportPreNotificationFixtures.ImportPreNotificationWithMissingComplementParameters();

        var decisionNotification = notification!.ToDecisionImportPreNotification();

        decisionNotification.Should().NotBeNull();
    }
}
