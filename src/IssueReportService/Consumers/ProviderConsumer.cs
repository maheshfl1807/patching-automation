namespace IssueReportService.Consumers
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Confluent.Kafka;
    using Data;
    using Entities;
    using LaunchSharp.Settings;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Platform.Model;
    using Settings;

    public class ProviderConsumer : AbstractConsumer<Ignore, string>
    {
        private readonly IDbContextFactory<IssueReportServiceContext> _contextFactory;
        private readonly ILogger _logger;

        public ProviderConsumer(
            ILogger logger,
            ISettings<KafkaSettings> kafkaSettings,
            IDbContextFactory<IssueReportServiceContext> contextFactory)
            : base(kafkaSettings)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            var groupId = kafkaSettings.GetRequired(s => s.ProviderConsumerGroupId);
            SetConfigGroupId(groupId);
        }

        /// <inheritdoc />
        public override async Task Consume(CancellationToken cancellationToken)
        {
            using var consumer = GetConsumer();
            consumer.Subscribe(Topics.PublicPlatformEntitiesProvidersV1);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

                    var consumeResult = consumer.Consume(cancellationToken);
                    var platformProvider = JsonConvert.DeserializeObject<Provider>(consumeResult.Message.Value);
                    var cloudProvider = await context.CloudProvider.SingleOrDefaultAsync(p => p.Name == platformProvider.Name);

                    if (cloudProvider == null)
                    {
                        cloudProvider = ConvertPlatformProviderToCloudProvider(platformProvider);
                        await context.CloudProvider.AddAsync(cloudProvider);
                    }

                    await context.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError("{Message}\n{Stack}", e.Message, e.StackTrace);
                }
            }

            consumer.Close();
        }

        private CloudProvider ConvertPlatformProviderToCloudProvider(Provider platformProvider)
        {
            return new CloudProvider
            {
                Name = platformProvider.Name,
            };
        }
    }
}