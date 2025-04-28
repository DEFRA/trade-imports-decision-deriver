using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Amazon.CloudWatch.EMF.Model;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Metrics;

[ExcludeFromCodeCoverage]
public static class MetricNames
{
    public const string MeterName = "Defra.TradeImportsDecisionDeriver.Deriver";
}

[ExcludeFromCodeCoverage]
public class ConsumerMetrics
{
    private readonly Histogram<double> consumeDuration;
    private readonly Counter<long> consumeTotal;
    private readonly Counter<long> consumeFaultTotal;
    private readonly Counter<long> consumerInProgress;

    public ConsumerMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MetricNames.MeterName);
        consumeTotal = meter.CreateCounter<long>(
            "MessagingConsume",
            Unit.COUNT.ToString(),
            description: "Number of messages consumed"
        );
        consumeFaultTotal = meter.CreateCounter<long>(
            "MessagingConsumeErrors",
            Unit.COUNT.ToString(),
            description: "Number of message consume faults"
        );
        consumerInProgress = meter.CreateCounter<long>(
            "MessagingConsumeActive",
            Unit.COUNT.ToString(),
            description: "Number of consumers in progress"
        );
        consumeDuration = meter.CreateHistogram<double>(
            "MessagingConsumeDuration",
            Unit.MILLISECONDS.ToString(),
            "Elapsed time spent consuming a message, in millis"
        );
    }

    public void Start(string path, string consumerName, string resourceType, string? subResourceType)
    {
        var tagList = BuildTags(path, consumerName, resourceType, subResourceType);

        consumeTotal.Add(1, tagList);
        consumerInProgress.Add(1, tagList);
    }

    public void Faulted(
        string queueName,
        string consumerName,
        string resourceType,
        string? subResourceType,
        Exception exception
    )
    {
        var tagList = BuildTags(queueName, consumerName, resourceType, subResourceType);

        tagList.Add(Constants.Tags.ExceptionType, exception.GetType().Name);
        consumeFaultTotal.Add(1, tagList);
    }

    public void Complete(
        string queueName,
        string consumerName,
        double milliseconds,
        string resourceType,
        string? subResourceType
    )
    {
        var tagList = BuildTags(queueName, consumerName, resourceType, subResourceType);

        consumerInProgress.Add(-1, tagList);
        consumeDuration.Record(milliseconds, tagList);
    }

    private static TagList BuildTags(string path, string consumerName, string resourceType, string? subResourceType)
    {
        return new TagList
        {
            { Constants.Tags.Service, Process.GetCurrentProcess().ProcessName },
            { Constants.Tags.QueueName, path },
            { Constants.Tags.ConsumerType, consumerName },
            { Constants.Tags.ResourceType, resourceType },
            { Constants.Tags.SubResourceType, subResourceType },
        };
    }

    public static class Constants
    {
        public static class Tags
        {
            public const string QueueName = "messaging.queue_name";
            public const string ConsumerType = "messaging.consumer_type";
            public const string Service = "messaging.service";
            public const string ExceptionType = "messaging.exception_type";
            public const string ResourceType = "messaging.resource_type";
            public const string SubResourceType = "messaging.sub_message_type";
        }
    }
}
