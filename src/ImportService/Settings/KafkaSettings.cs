namespace ImportService.Settings
{
    using System.Collections.Generic;
    using Common.Settings;
    using Confluent.Kafka.Admin;

    /// <inheritdoc />
    public class KafkaSettings : CommonKafkaSettings
    {
        public string AccountConsumerGroupId { get; set; }

        public int AccountConsumerCount { get; set; }

        public string ProviderConsumerGroupId { get; set; }

        public int ProviderConsumerCount { get; set; }

        public string ImportCommandConsumerGroupId { get; set; }

        public int ImportCommandConsumerCount { get; set; }

        public IEnumerable<TopicSpecification> Topics { get; set; }
    }
}