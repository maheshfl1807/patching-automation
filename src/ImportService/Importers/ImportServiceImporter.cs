namespace ImportService.Importers
{
    using System.Collections.Generic;
    using ImportService.Entities;

    public class ImportServiceImporter : IImporter
    {
        /// <inheritdoc />
        public void Import(IEnumerable<CloudServer> cloudServers)
        {
            // Set up context and save to database.
            throw new System.NotImplementedException();
        }
    }
}