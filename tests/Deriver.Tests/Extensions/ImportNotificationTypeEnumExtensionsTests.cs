using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

// ReSharper disable InconsistentNaming

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Extensions;

public class ImportNotificationTypeEnumExtensionsTests
{
    [Theory]
    [InlineData("9115", ImportNotificationType.Chedpp)]
    [InlineData("C633", ImportNotificationType.Chedpp)]
    [InlineData("N002", ImportNotificationType.Chedpp)]
    [InlineData("N851", ImportNotificationType.Chedpp)]
    [InlineData("C085", ImportNotificationType.Chedpp)]
    [InlineData("N852", ImportNotificationType.Ced)]
    [InlineData("C678", ImportNotificationType.Ced)]
    [InlineData("C640", ImportNotificationType.Cveda)]
    [InlineData("C641", ImportNotificationType.Cvedp)]
    [InlineData("C673", ImportNotificationType.Cvedp)]
    [InlineData("N853", ImportNotificationType.Cvedp)]
    [InlineData("9HCG", null)]
    [InlineData("INVALID", null)]
    public void GetChedTypeTest(string documentCode, ImportNotificationType? expectedImportNotificationType)
    {
        documentCode.GetChedType().Should().Be(expectedImportNotificationType);
    }
}
