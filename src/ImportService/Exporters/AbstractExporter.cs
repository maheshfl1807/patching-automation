namespace ImportService.Exporters
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Castle.Core.Internal;
    using ImportService.Data;
    using ImportService.Entities;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Abstract class that includes exporter functionality used across most exporters.
    /// </summary>
    public abstract class AbstractExporter : IExporter
    {
        private readonly IDbContextFactory<ImportServiceContext> _contextFactory;
        private readonly string _providerName;

        private CloudProvider _cachedProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractExporter"/> class.
        /// </summary>
        /// <param name="contextFactory">ImportServiceContext factory.</param>
        /// <param name="providerName">Name of CloudProvider entity associated with parent exporter.</param>
        protected AbstractExporter(
            IDbContextFactory<ImportServiceContext> contextFactory,
            string providerName = null)
        {
            _contextFactory = contextFactory;
            _providerName = providerName;

            if (this._providerName.IsNullOrEmpty())
            {
                throw new Exception("Provider name must be specified when inheriting from AbstractExporter.");
            }
        }

        /// <inheritdoc/>
        public abstract Task<IEnumerable<CloudAccount>> GetCloudAccounts(
            IEnumerable<string> accountIdsFilter);

        /// <inheritdoc />
        public abstract Task<IEnumerable<CloudServer>> GetCloudServers(
            CloudAccount account,
            IEnumerable<string> serverIdsFilter);

        /// <inheritdoc/>
        public async Task<CloudProvider> GetCloudProvider()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            _cachedProvider ??= await context.CloudProvider.FirstOrDefaultAsync(
                p => p.Name == _providerName);

            return _cachedProvider;
        }
    }
}