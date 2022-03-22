namespace ServerReportService.Data
{
    using System.Threading;
    using System.Threading.Tasks;
    using LaunchSharp.Settings;
    using Microsoft.EntityFrameworkCore;
    using Settings;

    public class ServerReportServiceContextFactory : IDbContextFactory<ServerReportServiceContext>
    {
        private ISettings<MysqlSettings> _mysqlSettings;

        public ServerReportServiceContextFactory(ISettings<MysqlSettings> mysqlSettings)
        {
            _mysqlSettings = mysqlSettings;
        }

        /// <inheritdoc />
        public ServerReportServiceContext CreateDbContext()
        {
            var connectionString = _mysqlSettings.GetRequired(s => s.ConnectionString);
            var optionsBuilder = new DbContextOptionsBuilder<DbContext>();
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            return new ServerReportServiceContext(optionsBuilder.Options);
        }

        /// <inheritdoc />
        public Task<ServerReportServiceContext> CreateDbContextAsync(CancellationToken cancellationToken = new ())
            => Task.FromResult(CreateDbContext());
    }
}