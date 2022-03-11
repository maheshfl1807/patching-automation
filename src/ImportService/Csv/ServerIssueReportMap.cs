namespace ImportService.Csv
{
    using CsvHelper.Configuration;

    public sealed class ServerIssueReportMap : ClassMap<ServerIssueReport>
    {
        public ServerIssueReportMap()
        {
            Map(m => m.ProviderName).Index(0).Name("providerName");
            Map(m => m.AccountId).Index(1).Name("accountId");
            Map(m => m.ServerId).Index(2).Name("instanceId");
            Map(m => m.Issue).Index(3).Name("issue");
        }
    }
}