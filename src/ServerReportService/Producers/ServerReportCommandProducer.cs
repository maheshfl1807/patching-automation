namespace ServerReportService.Producers
{
    using System;
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
            this._logger = logger;
            var serverReportCommandMessage = rootSettings.Get(s => s.ServerReportCommandMessage);
            this._serverReportCommandMessage = serverReportCommandMessage;
        }

        public override async Task Produce()
        {
            var topic = Topics.PublicServerReportServiceCommandsServerReportV1;
            if (string.IsNullOrEmpty(this._serverReportCommandMessage)) // GetRequired should throw if null, also check for empty string
            {
                throw new ArgumentNullException("Missing input command, no 'ServerReportCommandMessage' was provided");
            }
            this._logger.LogInformation(
                "Submitting the following message to Kafka topic {Topic}:\n{Message}", topic, this._serverReportCommandMessage);

            using var producer = GetProducer();
            await producer.ProduceAsync(this._serverReportCommandMessage, new Message<Null, string>
            {
                Value = this._serverReportCommandMessage,
            });
        }
    }
}