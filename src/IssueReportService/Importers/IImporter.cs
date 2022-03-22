namespace IssueReportService.Importers
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Csv;
    using Entities;

    public interface IImporter
    {
        /// <summary>
        /// Import the IEnumerable of CloudServers into the implementing service.
        /// </summary>
        /// <param name="cloudServers">IEnumerable of CloudServer entities.</param>
        /// <param name="cloudAccount">CloudAccount of the list of CloudServer.</param>
        /// <param name="cloudProvider">CloudProvider of the list of CloudServer.</param>
        /// <param name="serverIssues">List supplied by higher-level process to track issues encountered with servers.</param>
        /// <param name="accountsWithCredentialIssues">List supplied by higher-level process to track accounts with credential issues.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task Import(
            IEnumerable<CloudServer> cloudServers,
            CloudAccount cloudAccount,
            CloudProvider cloudProvider,
            ConcurrentBag<ServerIssueReport> serverIssues,
            ConcurrentDictionary<string, bool> accountsWithCredentialIssues);
    }
}