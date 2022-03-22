namespace IssueReportService.Csv
{
    using CsvHelper.Configuration;

    public sealed class ServerIssueReportMap : ClassMap<ServerIssueReport>
    {
        public ServerIssueReportMap()
        {
            Map(m => m.ProviderName).Index(0).Name("providerName");
            Map(m => m.AccountId).Index(1).Name("accountId");
            Map(m => m.Region).Index(2).Name("region");
            Map(m => m.Profile).Index(3).Name("profile");
            Map(m => m.ServerId).Index(4).Name("instanceId");
            Map(m => m.Status).Index(4).Name("status");
            Map(m => m.Issue).Index(5).Name("issue");
        }
    }
}