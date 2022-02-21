namespace ImportService.Consumers
{
    using System.Threading;
    using System.Threading.Tasks;
    using Confluent.Kafka;
    using ImportService.Data;
    using ImportService.Settings;
    using LaunchSharp.Settings;
    using Microsoft.EntityFrameworkCore;

    public abstract class AbstractConsumer<TKey, TValue> : IConsumer
    {
        private ConsumerConfig _config;

        protected IDbContextFactory<ImportServiceContext> ContextFactory;

        protected string ServiceDomain;

        public AbstractConsumer(
            ISettings<KafkaSettings> kafkaSettings,
            IDbContextFactory<ImportServiceContext> contextFactory)
        {
            // TODO: Move this config to DependencyPackage similar to AdminClientBuilder.
            _config = new ConsumerConfig
            {
                BootstrapServers = kafkaSettings.GetRequired(s => s.BootstrapServers),
                AutoOffsetReset = AutoOffsetReset.Earliest,
            };
            ContextFactory = contextFactory;
            ServiceDomain = kafkaSettings.GetRequired(s => s.ServiceDomain);
        }

        public IConsumer<TKey, TValue> GetConsumer()
        {
            return new ConsumerBuilder<TKey, TValue>(_config).Build();
        }

        /// <inheritdoc />
        public abstract Task Consume(CancellationToken cancellationToken);

        protected void SetConfigGroupId(string groupId)
        {
            _config.GroupId = $"{ServiceDomain}.{groupId}";
        }
    }
}