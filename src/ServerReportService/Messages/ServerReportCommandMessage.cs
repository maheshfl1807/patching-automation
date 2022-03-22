namespace ServerReportService.Messages
{
    using System.Collections.Generic;

    public class ServerReportCommandMessage
    {
        public IEnumerable<string> AccountIds { get; set; }

        public IEnumerable<string> ProviderNames { get; set; }

        public IEnumerable<string> ServerIds { get; set; }
    }
}