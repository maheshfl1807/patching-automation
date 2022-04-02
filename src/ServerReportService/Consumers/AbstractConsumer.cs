namespace ServerReportService.Consumers
{
    using System.Threading;
    using System.Threading.Tasks;
    using Confluent.Kafka;
    using LaunchSharp.Settings;
    using Settings;

    public abstract class AbstractConsumer<TKey, TValue> : IConsumer
    {
        private readonly ConsumerConfig _config;

        private readonly string _serviceDomain;

        protected AbstractConsumer(ISettings<KafkaSettings> kafkaSettings)
        {
            _config = new ConsumerConfig
            {
                Debug = kafkaSettings.GetRequired(s => s.DebugSettings),
                BootstrapServers = kafkaSettings.GetRequired(s => s.BootstrapServers),
                AutoOffsetReset = AutoOffsetReset.Earliest,
            };
            _serviceDomain = kafkaSettings.GetRequired(s => s.ServiceDomain);
        }

        /// <inheritdoc />
        public abstract Task Consume(CancellationToken cancellationToken);

        /// <summary>
        /// Gets a new instance of a Kafka consumer.
        /// </summary>
        /// <returns>Instance of Kafka IConsumer.</returns>
        protected IConsumer<TKey, TValue> GetConsumer()
        {
            return new ConsumerBuilder<TKey, TValue>(_config).Build();
        }

        /// <summary>
        /// Sets the group id in config for the consumer group.
        /// </summary>
        /// <param name="groupId">Group id.</param>
        protected void SetConfigGroupId(string groupId)
        {
            _config.GroupId = $"{_serviceDomain}.{groupId}";
        }
    }
}