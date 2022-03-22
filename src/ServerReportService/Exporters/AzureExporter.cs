namespace ServerReportService.Exporters
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Data;
    using Entities;
    using Microsoft.EntityFrameworkCore;

    public class AzureExporter : AbstractExporter
    {
        private const string c_providerName = "Azure";

        public AzureExporter(
            IDbContextFactory<ServerReportServiceContext> contextFactory)
            : base(contextFactory, c_providerName)
        {
        }

        /// <inheritdoc />
        public override async Task<IEnumerable<CloudAccount>> GetCloudAccounts(
            IEnumerable<string> accountIdsFilter)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override async Task<IEnumerable<CloudServer>> GetCloudServers(
            CloudAccount account,
            IEnumerable<string> serverIdsFilter,
            ConcurrentDictionary<string, bool> accountsWithCredentialIssues)
        {
            throw new System.NotImplementedException();
        }
    }
}