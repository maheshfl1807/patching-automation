namespace ImportService.Data
{
    using System.IO;
    using Common.Entities;
    using ImportService.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata;

    public class ImportServiceContext : DbContext
    {
        public ImportServiceContext()
        {
        }

        public ImportServiceContext(DbContextOptions dbContextOptions)
        : base()
        {
        }

        public DbSet<CloudAccount> CloudAccount { get; set; }

        public DbSet<CloudProvider> CloudProvider { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // TODO: Move this to settings.
            var connectionString =
                "server=localhost; port=33306; database=importservice; user=root; password=dev; Persist Security Info=False; Connect Timeout=300";
            // var connectionString = this.mysqlSettings.GetRequired(s => s.ConnectionString);
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureRelationships(modelBuilder);

            ConfigureMetadataProperties<CloudAccount>(modelBuilder);
            ConfigureMetadataProperties<CloudProvider>(modelBuilder);
            ConfigureMetadataProperties<CloudServer>(modelBuilder);
        }

        private void ConfigureRelationships(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CloudAccount>()
                .HasOne(p => p.CloudProvider)
                .WithMany(b => b.CloudAccounts)
                .HasForeignKey(p => p.CloudProviderId);

            modelBuilder.Entity<CloudProvider>();

            modelBuilder.Entity<CloudServer>()
                .HasOne(s => s.CloudAccount)
                .WithMany(a => a.CloudServers)
                .HasForeignKey(s => s.CloudAccountId);
        }

        private void ConfigureMetadataProperties<TEntity>(ModelBuilder modelBuilder)
            where TEntity : AbstractAutoIncrementWithMetadata
        {
            modelBuilder.Entity<TEntity>().Property(e => e.CreatedAt).ValueGeneratedOnAdd();
            modelBuilder.Entity<TEntity>().Property(e => e.UpdatedAt).ValueGeneratedOnAddOrUpdate();

            modelBuilder.Entity<TEntity>().Property(e => e.CreatedAt).Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
            modelBuilder.Entity<TEntity>().Property(e => e.CreatedAt).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            modelBuilder.Entity<TEntity>().Property(e => e.UpdatedAt).Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
            modelBuilder.Entity<TEntity>().Property(e => e.UpdatedAt).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        }

        private string GetBaseProjectDirectory()
        {
            string path = null;
            string prevDirectory = null;
            var currentDirectory = Directory.GetCurrentDirectory();

            while (string.IsNullOrEmpty(path))
            {
                var substringStartPos = currentDirectory.LastIndexOf(Path.DirectorySeparatorChar) + 1;
                if (currentDirectory.Substring(substringStartPos) == "src")
                {
                    path = prevDirectory;
                }
                else
                {
                    prevDirectory = currentDirectory;
                    currentDirectory = Directory.GetParent(currentDirectory).FullName;
                }
            }

            return path;
        }
    }
}