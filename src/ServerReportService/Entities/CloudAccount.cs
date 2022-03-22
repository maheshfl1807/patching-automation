namespace ServerReportService.Entities
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Entity that represents an account in any cloud provider.
    /// </summary>
    [Index(nameof(CloudProviderAccountId), IsUnique = true)]
    public class CloudAccount : AbstractAutoIncrementWithMetadata
    {
        /// <summary>
        /// Gets or sets the internal cloud provider id relating to CloudProvider Id.
        /// </summary>
        public uint CloudProviderId { get; set; }

        /// <summary>
        /// Gets or sets the external account id supplied by the cloud provider.
        /// </summary>
        [Required]
        public string CloudProviderAccountId { get; set; }

        /// <summary>
        /// Gets or sets the cloud account's cloud provider.
        /// </summary>
        public CloudProvider CloudProvider { get; set; }

        /// <summary>
        /// Gets or sets the cloud account's servers.
        /// </summary>
        public IEnumerable<CloudServer> CloudServers { get; set; }
    }
}