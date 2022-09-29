namespace ServerReportService
{
    using Amazon.S3.Transfer;
    using Confluent.Kafka;
    using Consumers;
    using Consumers.Commands;
    using Data;
    using Exporters;
    using Importers;
    using ServerReportService.Producers;
    using LaunchSharp;
    using LaunchSharp.AccountAccess;
    using LaunchSharp.AccountAccess.AmazonIAM;
    using LaunchSharp.DependencyContainer.SimpleInjector.Packaging;
    using LaunchSharp.Patterns.DownloadToS3;
    using LaunchSharp.Settings;
    using Microsoft.EntityFrameworkCore;
    using Settings;
    using SimpleInjector;

    /// <summary>
    /// Controls which concrete classes are injected for interfaces.
    /// </summary>
    internal class DependencyPackage : IPackage
    {
        /// <summary>
        /// Define which concrete classes are injected for interfaces.
        /// </summary>
        /// <param name="container">The dependency injection container.</param>
        public void RegisterServices(Container container)
        {
            container.Register<Application>();
            container.Register<PlatformConnectionFactory>(Lifestyle.Singleton);
            container.Register<CredentialHandler>(Lifestyle.Singleton);
            container.Register<ServerReportCommandConsumer>(Lifestyle.Singleton);
            container.Register<ServerReportCommandProducer>(Lifestyle.Singleton);

            // Kafka
            container.Register(
                () => new AdminClientBuilder(new AdminClientConfig
                {
                    Debug = container.GetInstance<ISettings<KafkaSettings>>()
                        .GetRequired(s => s.DebugSettings),
                    BootstrapServers = container.GetInstance<ISettings<KafkaSettings>>()
                        .GetRequired(s => s.BootstrapServers),
                }),
                Lifestyle.Singleton);


            // Producers
            // // TODO: Delete IPlatformProducer producers as we add them into creation processes in Platform API.
            // container.Collection.Append<IPlatformProducer, ProviderProducer>(Lifestyle.Singleton);
            //
            // // Consumers
            // container.Collection.Append<IConsumer, AccountConsumer>(Lifestyle.Singleton);
            // container.Collection.Append<IConsumer, ProviderConsumer>(Lifestyle.Singleton);
            container.Collection.Append<IConsumer, ServerReportCommandConsumer>(Lifestyle.Singleton);

            // Exporters
            container.Collection.Append<IExporter, AwsExporter>(Lifestyle.Singleton);

            // Importers
            container.Collection.Append<IImporter, ServerReportImporter>(Lifestyle.Singleton);

            // Amazon Security
            container.Register<IAccountSecretArnProvider<AmazonCredentials>, AccountSecretArnProvider>(
                Lifestyle.Singleton);

            // Import Service Context
            container.Register<IDbContextFactory<ServerReportServiceContext>, ServerReportServiceContextFactory>(
                Lifestyle.Singleton);

            container.Register<IFactory<ITransferUtility>, TransferUtilityFactory>(Lifestyle.Singleton);
        }
    }
}
