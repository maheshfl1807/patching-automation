namespace IssueReportService.Messages
{
    using System.Collections.Generic;

    public class IssueReportCommandMessage
    {
        public IEnumerable<string> AccountIds { get; set; }

        public IEnumerable<string> ProviderNames { get; set; }

        public IEnumerable<string> ServerIds { get; set; }
    }
}