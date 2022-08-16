namespace ServerReportService.Settings
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
        /// Comma delimited list of Kafka debug options. e.g. "generic,broker,topic,msg,consumer,cgrp,fetch"
        /// </summary>
        public string DebugSettings { get; set; }

        /// <summary>
        /// A unique identifier for the service that is using Kafka.
        /// </summary>
        public string ServiceDomain { get; set; }

        public short TopicReplicationFactor { get; set; }

        public string AccountConsumerGroupId { get; set; }

        public int AccountConsumerCount { get; set; }

        public string ProviderConsumerGroupId { get; set; }

        public int ProviderConsumerCount { get; set; }

        public string ServerReportCommandConsumerGroupId { get; set; }

        public int ServerReportCommandConsumerCount { get; set; }
    }
}