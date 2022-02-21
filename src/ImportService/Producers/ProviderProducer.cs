namespace ImportService.Producers
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
    public class ProviderProducer : AbstractProducer<Null, string>, IPlatformProducer
    {
        private readonly PlatformConnectionFactory _platformConnectionFactory;

        public ProviderProducer(
            ISettings<KafkaSettings> kafkaSettings,
            PlatformConnectionFactory platformConnectionFactory)
            : base(kafkaSettings)
        {
            _platformConnectionFactory = platformConnectionFactory;
        }

        /// <inheritdoc />
        public override async Task Produce()
        {
            using var producer = GetProducer();

            foreach (var provider in await GetAllPlatformProviders())
            {
                await producer.ProduceAsync(Topics.PublicPlatformEntitiesProviders, new Message<Null, string>
                {
                    Value = JsonConvert.SerializeObject(provider),
                });
                Console.WriteLine($"PROVIDER PRODUCER: {provider.Id}");
            }
        }

        private async Task<IEnumerable<Provider>> GetAllPlatformProviders()
        {
            await using var executor = _platformConnectionFactory.CreateConnection();

            const string sql = @"
                SELECT *
                FROM Provider p
            ";

            return await executor.QueryAsync<Provider>(sql);
        }
    }
}