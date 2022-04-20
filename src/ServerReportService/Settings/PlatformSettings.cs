namespace ServerReportService.Settings
{
    /// <summary>
    /// Settings related to Platform configuration.
    /// </summary>
    public class PlatformSettings
    {
        /// <summary>
        /// The JSON section key to use in appsettings.json.
        /// </summary>
        public const string Section = "Platform";

        /// <summary>
        /// Gets the connection string to the Platform DB.
        /// </summary>
        public string ConnectionString { get; set; }

    }
}