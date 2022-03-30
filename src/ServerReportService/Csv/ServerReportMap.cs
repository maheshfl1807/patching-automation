namespace ServerReportService.Csv
{
    using CsvHelper.Configuration;

    public sealed class ServerReportMap : ClassMap<ServerReport>
    {
        public ServerReportMap()
        {
            Map(m => m.ProviderName).Index(0).Name("providerName");
            Map(m => m.AccountId).Index(1).Name("accountId");
            Map(m => m.Region).Index(2).Name("region");
            Map(m => m.Profile).Index(3).Name("profile");
            Map(m => m.ServerId).Index(4).Name("instanceId");
            Map(m => m.ServerState).Index(4).Name("serverState");
            Map(m => m.PatchTagStatus).Index(5).Name("patchTagStatus");
            Map(m => m.SSMStatus).Index(5).Name("ssmStatus");
        }
    }
}