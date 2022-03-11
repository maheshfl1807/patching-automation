namespace ImportService.Consumers.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;
    using Confluent.Kafka;
    using ImportService.Exporters;
    using ImportService.Importers;
    using ImportService.Messages;
    using ImportService.Settings;
    using LaunchSharp.Settings;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

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
            consumer.Subscribe(Topics.PublicImportServiceCommandsImportV1);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Import command consumer waiting for message to consume...");

                    var consumeResult = consumer.Consume(cancellationToken);
                    var importCommandMessage = JsonConvert.DeserializeObject<ImportCommandMessage>(consumeResult.Message.Value);

                    foreach (var exporter in _exporters)
                    {
                        var exporterProvider = await exporter.GetCloudProvider();
                        if (importCommandMessage.ProviderNames == null || importCommandMessage.ProviderNames.Contains(exporterProvider.Name))
                        {
                            var cloudAccounts = await exporter.GetCloudAccounts(importCommandMessage.AccountIds);
                            foreach (var cloudAccount in cloudAccounts)
                            {
                                var cloudServers = await exporter.GetCloudServers(cloudAccount, importCommandMessage.ServerIds);
                                foreach (var importer in _importers)
                                {
                                    await importer.Import(cloudServers, cloudAccount, exporterProvider);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("{Message}\n{Stack}", e.Message, e.StackTrace);
                }
            }

            consumer.Close();
        }
    }
}