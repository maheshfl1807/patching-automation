namespace ImportService.Data
{
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

        public ImportServiceContext CreateDbContext()
        {
            return new ImportServiceContext();
        }
    }
}