namespace ServerReportService
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using LaunchSharp.Settings;
    using Microsoft.Extensions.Logging;
    using ServerReportService.Consumers;
    using ServerReportService.Consumers.Commands;
    using ServerReportService.Settings;

    /// <summary>
    /// Application.
    /// </summary>
    internal class Application
    {
        /// <summary>
        /// Logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// List of consumers to run.
        /// </summary>
        private readonly IEnumerable<IConsumer> _consumers;

        /// <summary>
        /// Consumer class for server report.
        /// </summary>
        private readonly ServerReportCommandConsumer _serverReportCommandConsumer;

        /// <summary>
        /// Command message that contains instructions for what the application should do.
        /// </summary>
        private readonly string _serverReportCommandMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="Application"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="serverReportCommandConsumer">Consumer class for server report.</param>
        /// <param name="rootSettings">Root settings of the service.</param>
        public Application(
            ILogger logger,
            ServerReportCommandConsumer serverReportCommandConsumer,
            ISettings<RootSettings> rootSettings)
        {
            _logger = logger;
            _serverReportCommandConsumer = serverReportCommandConsumer;
            _serverReportCommandMessage = rootSettings.Get(s => s.ServerReportCommandMessage);
        }

        /// <summary>
        /// Application entrypoint.
        /// </summary>
        /// <returns>N/A.</returns>
        public async Task Run()
        {
            if (string.IsNullOrEmpty(this._serverReportCommandMessage))
            {
                throw new ArgumentNullException("Missing input command, no 'ServerReportCommandMessage' was provided");
            }

            try
            {
                await _serverReportCommandConsumer.ProcessCommandMessage(this._serverReportCommandMessage, new CancellationTokenSource().Token);
            }
            catch (Exception e)
            {
                _logger.LogError("{Message}\n{Stack}", e.Message, e.StackTrace);
            }
        }
    }
}
