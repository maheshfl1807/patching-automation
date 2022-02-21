namespace ImportService.Consumers.Commands
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;
    using Confluent.Kafka;
    using ImportService.Data;
    using ImportService.Entities;
    using ImportService.Exporters;
    using ImportService.Importers;
    using ImportService.Settings;
    using LaunchSharp.Settings;
    using Microsoft.EntityFrameworkCore;

    public class ImportCommandConsumer : AbstractConsumer<Ignore, string>
    {
        private readonly IEnumerable<IExporter> _exporters;
        private readonly IEnumerable<IImporter> _importers;

        public ImportCommandConsumer(
            ISettings<KafkaSettings> kafkaSettings,
            IDbContextFactory<ImportServiceContext> contextFactory,
            IEnumerable<IExporter> exporters,
            IEnumerable<IImporter> importers)
            : base(kafkaSettings, contextFactory)
        {
            _exporters = exporters;
            _importers = importers;
            var groupId = kafkaSettings.GetRequired(s => s.ImportCommandConsumerGroupId);
            SetConfigGroupId(groupId);
        }

        public override async Task Consume(CancellationToken cancellationToken)
        {
            using var consumer = GetConsumer();
            consumer.Subscribe(Topics.PublicImportServiceCommandsImport);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // TODO: Construct a data model for this and use it to control different aspects of importing.
                    // TODO: Maybe functionality like "only import AWS accounts" or "import these specific account ids"
                    var consumeResult = consumer.Consume(cancellationToken);

                    // TODO: Figure out why context operations can't be async without killing the app
                    // TODO: but giving a 0 exit code??
                    // await using var context = await this.contextFactory.CreateDbContextAsync(cancellationToken);

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
                    Console.WriteLine(e.Message);
                }
            }

            consumer.Close();
        }
    }
}