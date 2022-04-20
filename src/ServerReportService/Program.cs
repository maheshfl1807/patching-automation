namespace ServerReportService
{
    using LaunchSharp;
    using LaunchSharp.AccountAccess.AmazonIAM;
    using LaunchSharp.DependencyContainer.SimpleInjector;
    using LaunchSharp.DependencyContainer.SimpleInjector.Packaging;
    using LaunchSharp.Logging.Serilog;
    using Microsoft.Extensions.Logging;
    using Settings;

    /// <summary>
    /// Bootstrap class for the application.
    /// </summary>
    internal class Program
    {
        private static readonly Deployment LocalDeployment = new Deployment("local");

        /// <summary>
        /// Main entrypoint. Bootstraps the application.
        /// </summary>
        /// <param name="args">Command line args.</param>
        /// <returns>An exit code.</returns>
        public static int Main(string[] args)
        {
            return Build(args)
                .Run(app => app.Run());
        }

        private static EntryPoint<Application> Build(string[] args)
        {
            return EntryPoint.ForRoot<Application>()
                .WithCommandLineArguments(args)

                .If(DeploymentCondition.Is(LocalDeployment), builder => builder
                    .WithMinLogLevel(LogLevel.Trace))
                .Else(builder => builder
                    .WithMinLogLevel(LogLevel.Information)
                    .WithStructuredSerilog())

                .WithConfigurationBinding<RootSettings>()
                .WithConfigurationBinding<AccountAccessSettings>("AccountAccess")
                .WithConfigurationBinding<KafkaSettings>("Kafka")
                .WithConfigurationBinding<PlatformSettings>("Platform")

                .WithErrorReturnCode(1)

                .WithSimpleInjector(container =>
                {
                    container.RequirePackage<DependencyPackage>();
                })

                .Create();
        }
    }
}