namespace IssueReportService.Settings
{
    using System.Collections.Generic;
    using Confluent.Kafka.Admin;

    /// <summary>
    /// Settings related to Kafka configuration.
    /// </summary>
    public class KafkaSettings
    {
        /// <summary>
        /// Comma delimited list of Kafka server domains to connect to.
        /// </summary>
        public string BootstrapServers { get; set; }

        /// <summary>
        /// A unique identifier for the service that is using Kafka.
        /// </summary>
        public string ServiceDomain { get; set; }

        public string AccountConsumerGroupId { get; set; }

        public int AccountConsumerCount { get; set; }

        public string ProviderConsumerGroupId { get; set; }

        public int ProviderConsumerCount { get; set; }

        public string IssueReportCommandConsumerGroupId { get; set; }

        public int IssueReportCommandConsumerCount { get; set; }
    }
}