namespace IssueReportService
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon;
    using Confluent.Kafka;
    using Confluent.Kafka.Admin;
    using IssueReportService.Consumers;
    using IssueReportService.Producers;
    using IssueReportService.Settings;
    using LaunchSharp.Settings;
    using Microsoft.Extensions.Logging;
    using SmartFormat;

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

        private readonly CredentialHandler _credentialHandler;

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
            // IEnumerable<IPlatformProducer> platformProducers,
            CredentialHandler credentialHandler)
        {
            _logger = logger;
            _rootSettings = rootSettings;
            _kafkaSettings = kafkaSettings;
            _kafkaAdminClient = kafkaAdminClientBuilder.Build();
            _consumers = consumers;
            // _platformProducers = platformProducers;
            _credentialHandler = credentialHandler;
        }

        /// <summary>
        /// Application entrypoint.
        /// </summary>
        /// <returns>N/A.</returns>
        public async Task Run()
        {
            // Output message to run for all configured accounts.
            _logger.LogInformation(_credentialHandler.GetCommandMessage());
            var creds = _credentialHandler.GetAccountCredentials("006486291791", RegionEndpoint.USWest2);

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

            // // Start temporary Platform producers to initialize data.
            // foreach (var producer in _platformProducers)
            // {
            //     // No need to await since data is not required immediately
            //     #pragma warning disable CS4014
            //     await Task.Run(() => producer.Produce());
            //     #pragma warning restore CS4014
            // }

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
