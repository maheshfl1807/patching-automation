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
        /// <returns>List of cloud accounts.</returns>
        public Task<IEnumerable<CloudAccount>> GetCloudAccounts();

        /// <summary>
        /// Gets a list of servers using a cloud provider's API for a specific account.
        /// </summary>
        /// <param name="account">The cloud account to get servers for.</param>
        /// <returns>List of cloud servers.</returns>
        public Task<IEnumerable<CloudServer>> GetCloudServers(CloudAccount account);
    }
}