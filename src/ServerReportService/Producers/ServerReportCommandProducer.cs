namespace ServerReportService.Producers
{
    using System.Threading.Tasks;
    using Confluent.Kafka;
    using LaunchSharp.Settings;
    using Microsoft.Extensions.Logging;
    using ServerReportService.Settings;

    public class ServerReportCommandProducer : AbstractProducer<Null, string>
    {
        private readonly string _serverReportCommandMessage;
        private readonly ILogger _logger;

        public ServerReportCommandProducer(
            ISettings<RootSettings> rootSettings,
            ISettings<KafkaSettings> kafkaSettings,
            CredentialHandler credentialHandler,
            ILogger logger)
            : base(kafkaSettings)
        {
            _logger = logger;
            var serverReportCommandMessage = rootSettings.Get(s => s.ServerReportCommandMessage);
            _serverReportCommandMessage = string.IsNullOrEmpty(serverReportCommandMessage)
                ? credentialHandler.GetCommandMessage()
                : serverReportCommandMessage;
        }

        public override async Task Produce()
        {
            var topic = Topics.PublicServerReportServiceCommandsServerReportV1;
            var message = _serverReportCommandMessage;
            _logger.LogInformation(
                "Submitting the following message to Kafka topic {Topic}:\n{Message}", topic, message);

            using var producer = GetProducer();
            await producer.ProduceAsync(topic, new Message<Null, string>
            {
                Value = message,
            });
        }
    }
}