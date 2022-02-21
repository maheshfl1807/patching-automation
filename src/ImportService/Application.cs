namespace ImportService
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;
    using Confluent.Kafka;
    using Confluent.Kafka.Admin;
    using ImportService.Consumers;
    using ImportService.Producers;
    using ImportService.Settings;
    using LaunchSharp.Settings;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Application.
    /// </summary>
    internal class Application
    {
        /// <summary>
        /// Logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Root level application settings.
        /// </summary>
        private readonly ISettings<RootSettings> _rootSettings;

        /// <summary>
        /// Settings related to Kafka configuration.
        /// </summary>
        private readonly ISettings<KafkaSettings> _kafkaSettings;

        /// <summary>
        /// Admin client used for system operations in Kafka.
        /// </summary>
        private readonly IAdminClient _kafkaAdminClient;

        /// <summary>
        /// List of consumers to run.
        /// </summary>
        private readonly IEnumerable<IConsumer> _consumers;

        /// <summary>
        /// List of platform producers to emulate on startup.
        /// </summary>
        private readonly IEnumerable<IPlatformProducer> _platformProducers;

        /// <summary>
        /// Initializes a new instance of the <see cref="Application"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="rootSettings">Root level application settings.</param>
        /// <param name="kafkaSettings">Settings related to Kafka configuration.</param>
        /// <param name="kafkaAdminClientBuilder">Kafka AdminClient Builder.</param>
        /// <param name="consumers">List of consumers to run.</param>
        /// <param name="platformProducers">List of platform producers to emulate on startup.</param>
        public Application(
            ILogger logger,
            ISettings<RootSettings> rootSettings,
            ISettings<KafkaSettings> kafkaSettings,
            AdminClientBuilder kafkaAdminClientBuilder,
            IEnumerable<IConsumer> consumers,
            IEnumerable<IPlatformProducer> platformProducers)
        {
            _logger = logger;
            _rootSettings = rootSettings;
            _kafkaSettings = kafkaSettings;
            _kafkaAdminClient = kafkaAdminClientBuilder.Build();
            _consumers = consumers;
            _platformProducers = platformProducers;
        }

        /// <summary>
        /// Application entrypoint.
        /// </summary>
        /// <returns>N/A</returns>
        public async Task Run()
        {
            Console.WriteLine("Hello World");

            // Set up topics.
            var topicNames = typeof(Topics).GetFields(BindingFlags.Static | BindingFlags.Public)
                .Where(x => x.IsLiteral && !x.IsInitOnly)
                .Select(x => x.GetValue(null)).Cast<string>();
            var topicSpecifications = new List<TopicSpecification>();

            foreach (var topicName in topicNames)
            {
                // TODO: Figure out the best way to define things other than Name in settings.
                topicSpecifications.Add(new TopicSpecification
                {
                    Name = topicName,
                    ReplicationFactor = 1,
                    NumPartitions = 1,
                });
            }

            try
            {
                await _kafkaAdminClient.CreateTopicsAsync(topicSpecifications);
            }
            catch (CreateTopicsException e)
            {
                if (!e.Message.Contains("already exists"))
                {
                    _logger.LogCritical(e, "{Message}", e.Message);
                    Environment.Exit(-1);
                }
            }

            // Start temporary Platform producers to initialize data.
            // TODO: Delete these producers as we add them into creation processes in Platform API.
            foreach (var producer in _platformProducers)
            {
                // No need to await since data is not required immediately
                #pragma warning disable CS4014
                Task.Factory.StartNew(() => producer.Produce());
                #pragma warning restore CS4014
            }

            // Start consumers
            var consumerCancellationTokenSource = new CancellationTokenSource();
            var consumerTasks = new List<Task>();
            foreach (var consumer in _consumers)
            {
                consumerTasks.Add(Task.Factory.StartNew(
                    () => consumer.Consume(consumerCancellationTokenSource.Token), consumerCancellationTokenSource.Token));
            }

            Task.WaitAll(consumerTasks.ToArray());
        }
    }
}
