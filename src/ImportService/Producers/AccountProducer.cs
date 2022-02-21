﻿namespace ImportService.Producers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Common;
    using Confluent.Kafka;
    using Dapper;
    using ImportService.Data;
    using ImportService.Settings;
    using LaunchSharp.Settings;
    using Newtonsoft.Json;
    using Platform.Model;

    /// <summary>
    /// Temporary producer for cloud accounts using the platform DB.
    /// TODO: Delete this producer when one is added to account creation process.
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

            foreach (var platformAccount in await GetAllValidPlatformAccounts())
            {
                await producer.ProduceAsync(Topics.PublicPlatformEntitiesAccounts, new Message<Null, string>
                {
                    Value = JsonConvert.SerializeObject(platformAccount),
                });
                Console.WriteLine($"ACCOUNT PRODUCER: {platformAccount.Id}");
            }
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