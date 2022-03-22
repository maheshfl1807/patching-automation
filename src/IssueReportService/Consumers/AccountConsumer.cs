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

    public class AccountConsumer : AbstractConsumer<Ignore, string>
    {
        private readonly IDbContextFactory<IssueReportServiceContext> _contextFactory;
        private readonly ILogger _logger;

        public AccountConsumer(
            ILogger logger,
            ISettings<KafkaSettings> kafkaSettings,
            IDbContextFactory<IssueReportServiceContext> contextFactory)
            : base(kafkaSettings)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            var groupId = kafkaSettings.GetRequired(s => s.AccountConsumerGroupId);
            SetConfigGroupId(groupId);
        }

        /// <inheritdoc />
        public override async Task Consume(CancellationToken cancellationToken)
        {
            using var consumer = GetConsumer();
            consumer.Subscribe(Topics.PublicPlatformEntitiesAccountsV1);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

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
                        throw new Exception("Provider name was not specified.");
                    }

                    await context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception e)
                {
                    _logger.LogError("{Message}\n{Stack}", e.Message, e.StackTrace);
                }
            }

            consumer.Close();
        }

        private CloudAccount ConvertPlatformAccountToCloudAccount(Account platformAccount)
        {
            return new CloudAccount
            {
                CloudProviderAccountId = platformAccount.AccountId,
            };
        }
    }
}