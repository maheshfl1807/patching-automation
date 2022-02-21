namespace ImportService.Consumers
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;
    using Confluent.Kafka;
    using ImportService.Data;
    using ImportService.Entities;
    using ImportService.Settings;
    using LaunchSharp.Settings;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json;
    using Platform.Model;

    public class ProviderConsumer : AbstractConsumer<Ignore, string>
    {
        public ProviderConsumer(
            ISettings<KafkaSettings> kafkaSettings,
            IDbContextFactory<ImportServiceContext> contextFactory)
            : base(kafkaSettings, contextFactory)
        {
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
                    // TODO: Figure out why context operations can't be async without killing the app
                    // TODO: but giving a 0 exit code??
                    Console.WriteLine("Provider consumer waiting for message to consume...");
                    await using var context = await ContextFactory.CreateDbContextAsync(cancellationToken);

                    var consumeResult = consumer.Consume(cancellationToken);
                    var platformProvider = JsonConvert.DeserializeObject<Provider>(consumeResult.Message.Value);
                    var cloudProvider = context.CloudProvider.SingleOrDefault(p => p.Name == platformProvider.Name);

                    if (cloudProvider == null)
                    {
                        cloudProvider = ConvertPlatformProviderToCloudProvider(platformProvider);
                        context.CloudProvider.Add(cloudProvider);
                    }

                    // TODO: Figure out why this can't be async without killing loop.
                    context.SaveChanges();
                    Console.WriteLine(consumeResult.Message.Value);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
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