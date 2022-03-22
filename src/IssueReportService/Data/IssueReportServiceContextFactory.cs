namespace IssueReportService.Data
{
    using System.Threading;
    using System.Threading.Tasks;
    using IssueReportService.Settings;
    using LaunchSharp.Settings;
    using Microsoft.EntityFrameworkCore;

    public class IssueReportServiceContextFactory : IDbContextFactory<IssueReportServiceContext>
    {
        private ISettings<MysqlSettings> _mysqlSettings;

        public IssueReportServiceContextFactory(ISettings<MysqlSettings> mysqlSettings)
        {
            _mysqlSettings = mysqlSettings;
        }

        /// <inheritdoc />
        public IssueReportServiceContext CreateDbContext()
        {
            var connectionString = _mysqlSettings.GetRequired(s => s.ConnectionString);
            var optionsBuilder = new DbContextOptionsBuilder<DbContext>();
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            return new IssueReportServiceContext(optionsBuilder.Options);
        }

        /// <inheritdoc />
        public Task<IssueReportServiceContext> CreateDbContextAsync(CancellationToken cancellationToken = new ())
            => Task.FromResult(CreateDbContext());
    }
}