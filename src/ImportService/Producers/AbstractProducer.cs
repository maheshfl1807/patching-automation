namespace ImportService.Producers
{
    using System.Threading.Tasks;
    using Confluent.Kafka;
    using ImportService.Settings;
    using LaunchSharp.Settings;

    public abstract class AbstractProducer<TKey, TValue> : IProducer
    {
        private readonly ProducerConfig _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractProducer{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="kafkaSettings">Settings related to Kafka.</param>
        protected AbstractProducer(ISettings<KafkaSettings> kafkaSettings)
        {
            _config = new ProducerConfig
            {
                BootstrapServers = kafkaSettings.GetRequired(s => s.BootstrapServers),
            };
        }

        /// <summary>
        /// Returns a newly built instance of a Kafka producer with type {TKey, TValue}.
        /// </summary>
        /// <returns>Confluent.Kafka.IProducer.</returns>
        public IProducer<TKey, TValue> GetProducer()
        {
            return new ProducerBuilder<TKey, TValue>(_config).Build();
        }

        /// <inheritdoc />
        public abstract Task Produce();
    }
}