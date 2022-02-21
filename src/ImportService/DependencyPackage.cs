namespace ImportService
{
    using Confluent.Kafka;
    using ImportService.Consumers;
    using ImportService.Consumers.Commands;
    using ImportService.Data;
    using ImportService.Exporters;
    using ImportService.Importers;
    using ImportService.Producers;
    using ImportService.Settings;
    using LaunchSharp.AccountAccess;
    using LaunchSharp.AccountAccess.AmazonIAM;
    using LaunchSharp.DependencyContainer.SimpleInjector.Packaging;
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

            // Kafka
            container.Register<AdminClientBuilder>(
                () => new AdminClientBuilder(new AdminClientConfig
                {
                    BootstrapServers = container.GetInstance<ISettings<KafkaSettings>>()
                        .GetRequired(s => s.BootstrapServers),
                }),
                Lifestyle.Singleton);

            // Producers
            container.Collection.Append<IPlatformProducer, AccountProducer>(Lifestyle.Singleton);
            container.Collection.Append<IPlatformProducer, ProviderProducer>(Lifestyle.Singleton);

            // Consumers
            container.Collection.Append<IConsumer, AccountConsumer>(Lifestyle.Singleton);
            container.Collection.Append<IConsumer, ProviderConsumer>(Lifestyle.Singleton);
            container.Collection.Append<IConsumer, ImportCommandConsumer>(Lifestyle.Singleton);

            // Exporters
            container.Collection.Append<IExporter, AwsExporter>(Lifestyle.Singleton);

            // Importers
            container.Collection.Append<IImporter, InfraGuardImporter>(Lifestyle.Singleton);

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
            container.Register<IDbContextFactory<ImportServiceContext>, ImportServiceContextFactory>(
                Lifestyle.Singleton);

            // container.Register<ImportServiceContext>(
            //     () =>
            //     {
            //         var mysqlSettings = container.GetInstance<ISettings<MysqlSettings>>();
            //         var connectionString = mysqlSettings.GetRequired(s => s.ConnectionString);
            //         var optionsBuilder = new DbContextOptionsBuilder<DbContext>();
            //         optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            //
            //         return new ImportServiceContext(optionsBuilder.Options);
            //     });
        }
    }
}
