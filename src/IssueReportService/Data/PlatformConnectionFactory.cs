namespace IssueReportService.Data
{
    using LaunchSharp.Settings;
    using MySqlConnector;
    using Settings;

    public class PlatformConnectionFactory
    {
        private readonly ISettings<PlatformSettings> _platformSettings;

        public PlatformConnectionFactory(ISettings<PlatformSettings> platformSettings)
        {
            _platformSettings = platformSettings;
        }

        public MySqlConnection CreateConnection()
        {
            return new MySqlConnection(_platformSettings.GetRequired(s => s.ConnectionString));
        }
    }
}