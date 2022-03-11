namespace ImportService.Exporters
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using ImportService.Entities;

    /// <summary>
    /// Interface implemented by cloud provider to handle exporting of cloud servers.
    /// </summary>
    public interface IExporter
    {
        /// <summary>
        /// Get a list of accounts from Platform that correspond to the cloud provider.
        /// </summary>
        /// <param name="accountIdsFilter">Account ids to filter on.</param>
        /// <returns>List of cloud accounts.</returns>
        public Task<IEnumerable<CloudAccount>> GetCloudAccounts(
            IEnumerable<string> accountIdsFilter);

        /// <summary>
        /// Gets a list of servers using a cloud provider's API for a specific account.
        /// </summary>
        /// <param name="account">The cloud account to get servers for.</param>
        /// <param name="serverIdsFilter">Server ids to filter on.</param>
        /// <returns>List of cloud servers.</returns>
        public Task<IEnumerable<CloudServer>> GetCloudServers(
            CloudAccount account,
            IEnumerable<string> serverIdsFilter);

        /// <summary>
        /// Gets the provider entity associated with the exporter. Every exporter should
        /// be associated to a CloudProvider entity.
        /// </summary>
        /// <returns>The provider entity associated with the exporter.</returns>
        public Task<CloudProvider> GetCloudProvider();
    }
}