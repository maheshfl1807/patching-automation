namespace ServerReportService
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Confluent.Kafka;
    using Confluent.Kafka.Admin;
    using LaunchSharp.Settings;
    using Microsoft.Extensions.Logging;
    using ServerReportService.Consumers;
    using ServerReportService.Producers;
    using ServerReportService.Settings;

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
        /// Admin client used for system operations in Kafka.
        /// </summary>
        private readonly IAdminClient _kafkaAdminClient;

        /// <summary>
        /// List of consumers to run.
        /// </summary>
        private readonly IEnumerable<IConsumer> _consumers;

        /// <summary>
        /// Temporary producer for initial kickoff message until API is built.
        /// </summary>
        private readonly ServerReportCommandProducer _serverReportCommandProducer;

        /// <summary>
        /// Determines whether the service runs in producer or consumer mode.
        /// </summary>
        private readonly string _serverReportCommandMessage;

        /// <summary>
        /// The replication factor to apply to topics when created.
        /// </summary>
        private readonly short _topicReplicationFactor;

        /// <summary>
        /// Initializes a new instance of the <see cref="Application"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="kafkaAdminClientBuilder">Kafka AdminClient Builder.</param>
        /// <param name="consumers">List of consumers to run.</param>
        /// <param name="serverReportCommandProducer">Temporary producer for kickoff message.</param>
        /// <param name="rootSettings">Root settings of the service.</param>
        /// <param name="kafkaSettings">Kafka settings of the service.</param>
        public Application(
            ILogger logger,
            AdminClientBuilder kafkaAdminClientBuilder,
            IEnumerable<IConsumer> consumers,
            ServerReportCommandProducer serverReportCommandProducer,
            ISettings<RootSettings> rootSettings,
            ISettings<KafkaSettings> kafkaSettings)
        {
            _logger = logger;
            _kafkaAdminClient = kafkaAdminClientBuilder.Build();
            _consumers = consumers;
            _serverReportCommandProducer = serverReportCommandProducer;
            _serverReportCommandMessage = rootSettings.Get(s => s.ServerReportCommandMessage);
            _topicReplicationFactor = kafkaSettings.Get(s => s.TopicReplicationFactor, (short)2);
        }

        /// <summary>
        /// Application entrypoint.
        /// </summary>
        /// <returns>N/A.</returns>
        public async Task Run()
        {
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
                    ReplicationFactor = _topicReplicationFactor
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

            if (!string.IsNullOrEmpty(this._serverReportCommandMessage))
            {
                // Produce kickoff message
                await _serverReportCommandProducer.Produce();
            }
            else
            {
                // Start consumers
                var consumerCancellationTokenSource = new CancellationTokenSource();
                var consumerTasks = new List<Task>();
                foreach (var consumer in _consumers)
                {
                    consumerTasks.Add(Task.Run(
                        async () => await consumer.Consume(consumerCancellationTokenSource.Token), consumerCancellationTokenSource.Token));
                }

                await Task.WhenAll(consumerTasks.ToArray());
            }
        }
    }
}
