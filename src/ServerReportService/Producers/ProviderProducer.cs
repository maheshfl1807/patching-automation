namespace ServerReportService.Producers
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

            var platformProviders = await GetAllPlatformProviders();
            await platformProviders.ParallelForEachAsync(async platformProvider =>
            {
                await producer.ProduceAsync(Topics.PublicPlatformEntitiesProvidersV1, new Message<Null, string>
                {
                    Value = JsonConvert.SerializeObject(platformProvider),
                });
            });
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