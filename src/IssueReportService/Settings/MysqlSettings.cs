namespace IssueReportService.Settings
{
    /// <summary>
    /// Settings related to MySQL configuration.
    /// </summary>
    public class MysqlSettings
    {
        /// <summary>
        /// The JSON section key to use in appsettings.json.
        /// </summary>
        public const string Section = "Mysql";

        /// <summary>
        /// The version of MySQL being used. If not specified, it will be inferred from the connection string.
        /// </summary>
        public string ServerVersion { get; set; }

        /// <summary>
        /// The MySQL database connection string.
        /// </summary>
        public string ConnectionString { get; set; }
    }
}