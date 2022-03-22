namespace IssueReportService.Producers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Confluent.Kafka;
    using Dapper;
    using Dasync.Collections;
    using Data;
    using LaunchSharp.Settings;
    using Newtonsoft.Json;
    using Platform.Model;
    using Settings;

    /// <summary>
    /// Temporary producer for cloud accounts using the platform DB.
    /// </summary>
    public class AccountProducer : AbstractProducer<Null, string>, IPlatformProducer
    {
        private readonly PlatformConnectionFactory _platformConnectionFactory;

        public AccountProducer(
            ISettings<KafkaSettings> kafkaSettings,
            PlatformConnectionFactory dbExecutorFactory)
            : base(kafkaSettings)
        {
            _platformConnectionFactory = dbExecutorFactory;
        }

        /// <inheritdoc />
        public override async Task Produce()
        {
            using var producer = GetProducer();
            var platformAccounts = await GetAllValidPlatformAccounts();
            await platformAccounts.ParallelForEachAsync(async platformAccount =>
            {
                await producer.ProduceAsync(Topics.PublicPlatformEntitiesAccountsV1, new Message<Null, string>
                {
                    Value = JsonConvert.SerializeObject(platformAccount),
                });
            });
        }

        private async Task<IEnumerable<Account>> GetAllValidPlatformAccounts()
        {
            await using var executor = _platformConnectionFactory.CreateConnection();

            const string sql = @"
                SELECT a.*, p.Name as ProviderName
                FROM Account a
                JOIN Provider p on a.ProviderId = p.Id
                WHERE a.IsDeleted = 0
                AND (a.InactiveDate IS NULL OR a.InactiveDate > UTC_DATE())
            ";

            return await executor.QueryAsync<Account>(sql);
        }
    }
}