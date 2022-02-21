namespace Common.Settings
{
    /// <summary>
    /// Settings related to Kafka that are common across services.
    /// </summary>
    public abstract class CommonKafkaSettings
    {
        /// <summary>
        /// Comma delimited list of Kafka server domains to connect to.
        /// </summary>
        public string BootstrapServers { get; set; }

        /// <summary>
        /// A unique identifier for the service that is using Kafka.
        /// </summary>
        public string ServiceDomain { get; set; }
    }
}