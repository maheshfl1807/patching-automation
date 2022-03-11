namespace ImportService.Settings
{
    using System.Collections.Generic;
    using Common.Settings;

    /// <inheritdoc />
    public class RootSettings : CommonRootSettings
    {
        public IEnumerable<string> ValidPatchTagKeys { get; set; }

        public IEnumerable<string> InvalidPatchTagValues { get; set; }

        public string S3AccessPoint { get; set; }

        public string S3ReportKey { get; set; }

        public string SnsReportMessageTemplate { get; set; }

        public string SnsReportTopicArn { get; set; }

        public string SnsReportSubject { get; set; }
    }
}