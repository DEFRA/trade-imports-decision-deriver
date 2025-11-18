using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Configuration;

[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
public class AwsSqsOptions
{
    [Required]
    [ConfigurationKeyName("DATA_EVENTS_QUEUE_NAME")]
    public required string ResourceEventsQueueName { get; init; }

    public string ResourceEventsDeadLetterQueueName => $"{ResourceEventsQueueName}-deadletter";
}
