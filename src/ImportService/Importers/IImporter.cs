namespace ImportService.Importers
{
    using System.Collections.Generic;
    using ImportService.Entities;

    public interface IImporter
    {
        /// <summary>
        /// Import the IEnumerable of CloudServers into the underlying service.
        /// </summary>
        /// <param name="cloudServers">IEnumerable of CloudServer entities.</param>
        public void Import(IEnumerable<CloudServer> cloudServers);
    }
}