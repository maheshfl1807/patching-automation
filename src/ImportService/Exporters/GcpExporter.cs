namespace ImportService.Exporters
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using ImportService.Data;
    using ImportService.Entities;
    using Microsoft.EntityFrameworkCore;

    public class GcpExporter : AbstractExporter
    {
        private const string c_providerName = "GCP";

        public GcpExporter(IDbContextFactory<ImportServiceContext> contextFactory)
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
            IEnumerable<string> serverIdsFilter)
        {
            throw new System.NotImplementedException();
        }
    }
}