namespace ImportService.Importers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using ImportService.Entities;

    public interface IImporter
    {
        /// <summary>
        /// Import the IEnumerable of CloudServers into the implementing service.
        /// </summary>
        /// <param name="cloudServers">IEnumerable of CloudServer entities.</param>
        /// <param name="cloudAccount">CloudAccount of the list of CloudServer.</param>
        /// <param name="cloudProvider">CloudProvider of the list of CloudServer.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task Import(IEnumerable<CloudServer> cloudServers, CloudAccount cloudAccount, CloudProvider cloudProvider);
    }
}