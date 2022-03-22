namespace ServerReportService.Entities
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Entity that represents any cloud provider.
    /// </summary>
    [Index(nameof(Name), IsUnique = true)]
    public class CloudProvider : AbstractAutoIncrementWithMetadata
    {
        /// <summary>
        /// Gets or sets the cloud provider's name.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the cloud account's associated with the cloud provider.
        /// </summary>
        public IEnumerable<CloudAccount> CloudAccounts { get; set; }
    }
}