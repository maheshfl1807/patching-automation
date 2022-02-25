namespace ImportService.Data
{
    using System.Threading;
    using System.Threading.Tasks;
    using ImportService.Settings;
    using LaunchSharp.Settings;
    using Microsoft.EntityFrameworkCore;

    public class ImportServiceContextFactory : IDbContextFactory<ImportServiceContext>
    {
        private ISettings<MysqlSettings> _mysqlSettings;

        public ImportServiceContextFactory(ISettings<MysqlSettings> mysqlSettings)
        {
            _mysqlSettings = mysqlSettings;
        }

        /// <inheritdoc />
        public ImportServiceContext CreateDbContext()
        {
            var connectionString = _mysqlSettings.GetRequired(s => s.ConnectionString);
            var optionsBuilder = new DbContextOptionsBuilder<DbContext>();
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            return new ImportServiceContext(optionsBuilder.Options);
        }

        /// <inheritdoc />
        public Task<ImportServiceContext> CreateDbContextAsync(CancellationToken cancellationToken = new ())
            => Task.FromResult(CreateDbContext());
    }
}