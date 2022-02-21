namespace ImportService.Consumers
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
    using Newtonsoft.Json;
    using Platform.Model;

    public class AccountConsumer : AbstractConsumer<Ignore, string>
    {
        public AccountConsumer(
            ISettings<KafkaSettings> kafkaSettings,
            IDbContextFactory<ImportServiceContext> contextFactory)
            : base(kafkaSettings, contextFactory)
        {
            var groupId = kafkaSettings.GetRequired(s => s.AccountConsumerGroupId);
            SetConfigGroupId(groupId);
        }

        /// <inheritdoc />
        public override async Task Consume(CancellationToken cancellationToken)
        {
            using var consumer = GetConsumer();
            consumer.Subscribe(Topics.PublicPlatformEntitiesAccounts);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // TODO: Figure out why context operations can't be async without killing the app
                    // TODO: but giving a 0 exit code??
                    Console.WriteLine("Account consumer waiting for message to consume...");
                    await using var context = await ContextFactory.CreateDbContextAsync(cancellationToken);

                    var consumeResult = consumer.Consume(cancellationToken);
                    var platformAccount = JsonConvert.DeserializeObject<Account>(consumeResult.Message.Value);
                    var cloudAccount =
                        await context.CloudAccount.SingleOrDefaultAsync(
                            a => a.CloudProviderAccountId == platformAccount.AccountId, cancellationToken: cancellationToken);

                    if (cloudAccount == null)
                    {
                        cloudAccount = ConvertPlatformAccountToCloudAccount(platformAccount);
                        await context.CloudAccount.AddAsync(cloudAccount, cancellationToken);
                    }

                    if (!string.IsNullOrEmpty(platformAccount.ProviderName))
                    {
                        var provider = await context.CloudProvider.SingleOrDefaultAsync(p => p.Name == platformAccount.ProviderName, cancellationToken: cancellationToken);

                        if (provider == null)
                        {
                            cloudAccount.CloudProvider = new CloudProvider
                            {
                                Name = platformAccount.ProviderName,
                            };
                        }
                        else
                        {
                            cloudAccount.CloudProviderId = provider.Id;
                        }
                    }
                    else
                    {
                        // TODO: Throw exception or log, provider name is empty.
                    }

                    await context.SaveChangesAsync(cancellationToken);
                    Console.WriteLine(consumeResult.Message.Value);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            consumer.Close();
        }

        private CloudAccount ConvertPlatformAccountToCloudAccount(Account platformAccount)
        {
            return new CloudAccount
            {
                // TODO
                CloudProviderAccountId = platformAccount.AccountId,
            };
        }
    }
}