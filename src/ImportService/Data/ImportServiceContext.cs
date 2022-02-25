﻿namespace ImportService.Data
{
    using Common.Entities;
    using Common.Settings;
    using ImportService.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata;
    using Microsoft.Extensions.Configuration;

    /// <inheritdoc />
    public class ImportServiceContext : DbContext
    {
        /// <summary>
        /// Parameterless initialization identifier.
        /// </summary>
        public const string ParameterlessInitializationType = "Parameterless";

        /// <summary>
        /// With options initialization identifier.
        /// </summary>
        public const string WithOptionsInitializationType = "WithOptions";

        private readonly string _initializationType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImportServiceContext"/> class.
        /// </summary>
        public ImportServiceContext()
        {
            _initializationType = ParameterlessInitializationType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImportServiceContext"/> class.
        /// </summary>
        /// <param name="dbContextOptions">Database options.</param>
        public ImportServiceContext(DbContextOptions dbContextOptions)
        : base(dbContextOptions)
        {
            _initializationType = WithOptionsInitializationType;
        }

        public DbSet<CloudAccount> CloudAccount { get; set; }

        public DbSet<CloudProvider> CloudProvider { get; set; }

        /// <inheritdoc />
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (_initializationType.Equals(ParameterlessInitializationType))
            {
                // Yes, this is ugly. It's only necessary to run when applying migrations using the
                // "dotnet ef database update" command, which uses the parameterless constructor and
                // doesn't implement the DI container.
                var connectionString = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", true)
                    .AddJsonFile("appsettings.local.json", true)
                    .Build()
                    .GetSection(CommonMysqlSettings.Section)[nameof(CommonMysqlSettings.ConnectionString)];

                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            }
        }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureRelationships(modelBuilder);

            ConfigureMetadataProperties<CloudAccount>(modelBuilder);
            ConfigureMetadataProperties<CloudProvider>(modelBuilder);
            ConfigureMetadataProperties<CloudServer>(modelBuilder);
            ConfigureMetadataProperties<CloudServerTag>(modelBuilder);
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
    }
}