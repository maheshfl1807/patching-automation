namespace ImportService.Consumers.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;
    using Confluent.Kafka;
    using ImportService.Exporters;
    using ImportService.Importers;
    using ImportService.Settings;
    using LaunchSharp.Settings;
    using Microsoft.Extensions.Logging;

    public class ImportCommandConsumer : AbstractConsumer<Ignore, string>
    {
        private readonly ILogger _logger;
        private readonly IEnumerable<IExporter> _exporters;
        private readonly IEnumerable<IImporter> _importers;

        public ImportCommandConsumer(
            ISettings<KafkaSettings> kafkaSettings,
            ILogger logger,
            IEnumerable<IExporter> exporters,
            IEnumerable<IImporter> importers)
            : base(kafkaSettings)
        {
            _logger = logger;
            _exporters = exporters;
            _importers = importers;
            var groupId = kafkaSettings.GetRequired(s => s.ImportCommandConsumerGroupId);
            SetConfigGroupId(groupId);
        }

        /// <inheritdoc />
        public override async Task Consume(CancellationToken cancellationToken)
        {
            using var consumer = GetConsumer();
            consumer.Subscribe(Topics.PublicImportServiceCommandsImport);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Import command consumer waiting for message to consume...");

                    // TODO: Construct a data model for this and use it to control different aspects of importing.
                    // TODO: Maybe functionality like "only import AWS accounts" or "import these specific account ids"
                    var consumeResult = consumer.Consume(cancellationToken);

                    foreach (var exporter in _exporters)
                    {
                        var cloudAccounts = await exporter.GetCloudAccounts();
                        foreach (var cloudAccount in cloudAccounts)
                        {
                            var cloudServers = await exporter.GetCloudServers(cloudAccount);
                            foreach (var importer in _importers)
                            {
                                importer.Import(cloudServers);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    // TODO: Catch error.
                }
            }

            consumer.Close();
        }
    }
}