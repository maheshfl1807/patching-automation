namespace ImportService.Exporters
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using ImportService.Entities;

    public class AzureExporter : IExporter
    {
        /// <inheritdoc />
        public Task<IEnumerable<CloudAccount>> GetCloudAccounts()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public Task<IEnumerable<CloudServer>> GetCloudServers(CloudAccount account)
        {
            throw new System.NotImplementedException();
        }
    }
}