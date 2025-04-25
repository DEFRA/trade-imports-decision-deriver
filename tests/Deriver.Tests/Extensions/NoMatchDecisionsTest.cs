using Amazon.SQS.Model;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;
using SlimMessageBus.Host;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Extensions;

public class ConsumerContextExtensionsTests
{
    [Fact]
    public void WhenResourceTypeExists_ThenResourceTypeShouldBeReturned()
    {
        // Arrange
        var context = new ConsumerContext()
        {
            Headers = new Dictionary<string, object>()
            {
                { MessageBusHeaders.ResourceType, ResourceEventResourceTypes.CustomsDeclaration },
            },
        };

        // Act
        var resourceType = context.GetResourceType();

        // Assert
        resourceType.Should().Be(ResourceEventResourceTypes.CustomsDeclaration);
    }

    [Fact]
    public void WhenResourceTypeNotExists_ThenEmptyShouldBeReturned()
    {
        // Arrange
        var context = new ConsumerContext() { Headers = new Dictionary<string, object>() };

        // Act
        var resourceType = context.GetResourceType();

        // Assert
        resourceType.Should().Be(string.Empty);
    }

    [Fact]
    public void WhenSubResourceTypeExists_ThenResourceTypeShouldBeReturned()
    {
        // Arrange
        var context = new ConsumerContext()
        {
            Headers = new Dictionary<string, object>()
            {
                { MessageBusHeaders.SubResourceType, ResourceEventSubResourceTypes.ClearanceRequest },
            },
        };

        // Act
        var resourceType = context.GetSubResourceType();

        // Assert
        resourceType.Should().Be(ResourceEventSubResourceTypes.ClearanceRequest);
    }

    [Fact]
    public void WhenSubResourceTypeNotExists_ThenEmptyShouldBeReturned()
    {
        // Arrange
        var context = new ConsumerContext() { Headers = new Dictionary<string, object>() };

        // Act
        var resourceType = context.GetSubResourceType();

        // Assert
        resourceType.Should().Be(string.Empty);
    }

    [Fact]
    public void WhenMessageIdExists_ThenMessageIdShouldBeReturned()
    {
        // Arrange
        var context = new ConsumerContext()
        {
            Properties = new Dictionary<string, object>()
            {
                {
                    MessageBusHeaders.SqsBusMessage,
                    new Message() { MessageId = "TestMessageId" }
                },
            },
        };

        // Act
        var messageId = context.GetMessageId();

        // Assert
        messageId.Should().Be("TestMessageId");
    }

    [Fact]
    public void WhenMessageIdNotExists_ThenEmptyShouldBeReturned()
    {
        // Arrange
        var context = new ConsumerContext() { Properties = new Dictionary<string, object>() };

        // Act
        var messageId = context.GetMessageId();

        // Assert
        messageId.Should().Be(string.Empty);
    }
}
