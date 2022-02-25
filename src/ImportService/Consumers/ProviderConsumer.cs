﻿namespace ImportService.Consumers
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;
    using Confluent.Kafka;
    using ImportService.Data;
    using ImportService.Entities;
    using ImportService.Settings;
    using LaunchSharp.Settings;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Platform.Model;

    public class ProviderConsumer : AbstractConsumer<Ignore, string>
    {
        private readonly IDbContextFactory<ImportServiceContext> _contextFactory;
        private readonly ILogger _logger;

        public ProviderConsumer(
            ILogger logger,
            ISettings<KafkaSettings> kafkaSettings,
            IDbContextFactory<ImportServiceContext> contextFactory)
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
            consumer.Subscribe(Topics.PublicPlatformEntitiesProviders);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Provider consumer waiting for message to consume...");
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
                    _logger.LogError("{Message}", e.Message);
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