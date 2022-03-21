namespace IssueReportService.Settings
{
    using System.Collections.Generic;

    /// <summary>
    /// Root level service settings.
    /// </summary>
    public class RootSettings
    {
        /// <summary>
        /// The name of the service.
        /// </summary>
        public string ServiceName { get; set; }

        public Dictionary<string, string> AccountToExternalIdMap { get; set; }

        public IEnumerable<string> ValidPatchTagKeys { get; set; }

        public IEnumerable<string> InvalidPatchTagValues { get; set; }

        public string S3AccessPoint { get; set; }

        public string S3ReportKey { get; set; }

        public string SnsReportMessageTemplate { get; set; }

        public string SnsReportTopicArn { get; set; }

        public string SnsReportSubject { get; set; }
    }
}