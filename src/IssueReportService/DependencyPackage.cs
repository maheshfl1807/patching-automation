namespace IssueReportService
{
    using Amazon.S3.Transfer;
    using Confluent.Kafka;
    using IssueReportService.Consumers;
    using IssueReportService.Consumers.Commands;
    using IssueReportService.Data;
    using IssueReportService.Exporters;
    using IssueReportService.Importers;
    using IssueReportService.Importers.InfraGuard;
    using IssueReportService.Producers;
    using IssueReportService.Settings;
    using LaunchSharp;
    using LaunchSharp.AccountAccess;
    using LaunchSharp.AccountAccess.AmazonIAM;
    using LaunchSharp.DependencyContainer.SimpleInjector.Packaging;
    using LaunchSharp.Patterns.DownloadToS3;
    using LaunchSharp.Settings;
    using Microsoft.EntityFrameworkCore;
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
            container.Register<InfraGuardApi>(Lifestyle.Singleton);
            container.Register<CredentialHandler>(Lifestyle.Singleton);

            // Kafka
            container.Register(
                () => new AdminClientBuilder(new AdminClientConfig
                {
                    BootstrapServers = container.GetInstance<ISettings<KafkaSettings>>()
                        .GetRequired(s => s.BootstrapServers),
                }),
                Lifestyle.Singleton);

            // // Producers
            // // TODO: Delete IPlatformProducer producers as we add them into creation processes in Platform API.
            // container.Collection.Append<IPlatformProducer, AccountProducer>(Lifestyle.Singleton);
            // container.Collection.Append<IPlatformProducer, ProviderProducer>(Lifestyle.Singleton);
            //
            // // Consumers
            // container.Collection.Append<IConsumer, AccountConsumer>(Lifestyle.Singleton);
            // container.Collection.Append<IConsumer, ProviderConsumer>(Lifestyle.Singleton);
            container.Collection.Append<IConsumer, IssueReportCommandConsumer>(Lifestyle.Singleton);

            // Exporters
            container.Collection.Append<IExporter, AwsExporter>(Lifestyle.Singleton);

            // Importers
            // container.Collection.Append<IImporter, InfraGuardImporter>(Lifestyle.Singleton);
            container.Collection.Append<IImporter, IssueReportImporter>(Lifestyle.Singleton);

            // Amazon Security
            container.Register<IAccountSecretArnProvider<AmazonCredentials>, AccountSecretArnProvider>(
                Lifestyle.Singleton);
            container.Register<ICredentialsProvider<AmazonCredentials>, SSMAmazonCredentialsProvider>(
                Lifestyle.Singleton);
            container.Register<ICredentialCache<AmazonCredentials>, MemoizedCredentialCache<AmazonCredentials>>(
                Lifestyle.Singleton);
            container.Register<IAmazonSecurityTokenServiceFactory, AmazonSecurityTokenServiceFactory>(
                Lifestyle.Singleton);
            container.Register<IAmazonSimpleSystemsManagementFactory, AmazonSimpleSystemsManagementFactory>(
                Lifestyle.Singleton);
            container.Register<IRoleAssumer, IAMRoleAssumer>(Lifestyle.Singleton);
            container.Register<ISTSResponseHandler, STSResponseHandler>(Lifestyle.Singleton);

            // Import Service Context
            container.Register<IDbContextFactory<IssueReportServiceContext>, IssueReportServiceContextFactory>(
                Lifestyle.Singleton);

            container.Register<IFactory<ITransferUtility>, TransferUtilityFactory>(Lifestyle.Singleton);
        }
    }
}
