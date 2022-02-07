namespace ImportService
{
    using System;
    using System.Threading.Tasks;
    using LaunchSharp.Settings;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Application
    /// </summary>
    internal class Application
    {
        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// Settings
        /// </summary>
        private readonly ISettings<Settings> settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="Application"/> class.
        /// </summary>
        /// <param name="logger">The logger interface.</param>
        /// <param name="settings">The settings interface.</param>
        public Application(
            ILogger logger,
            ISettings<Settings> settings)
        {
            this.logger = logger;
            this.settings = settings;
        }

        /// <summary>
        /// Application entrypoint.
        /// </summary>
        /// <returns>N/A</returns>
        public async Task Run()
        {
            Console.WriteLine("Hello World");
        }
    }
}
