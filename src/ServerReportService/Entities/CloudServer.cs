namespace ServerReportService.Entities
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Entity that represents a server in any cloud provider.
    /// </summary>
    [Index(nameof(ServerId), nameof(CloudAccountId), IsUnique = true)]
    public class CloudServer : AbstractAutoIncrementWithMetadata
    {
        /// <summary>
        /// Gets or sets the internal cloud account id relating to CloudAccount Id.
        /// </summary>
        [Required]
        public uint CloudAccountId { get; set; }

        /// <summary>
        /// Gets or sets the cloud provider's globally unique identifier of the server.
        /// </summary>
        public string ServerId { get; set; }

        /// <summary>
        /// Gets or sets the cloud provider's profile identifier of the server.
        /// </summary>
        public string ProfileId { get; set; }

        /// <summary>
        /// Gets or sets the region of the cloud provider that the server is located in.
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// Gets or sets if the server is currently running or not.
        /// </summary>
        public bool IsRunning { get; set; }

        /// <summary>
        /// Gets or sets the cloud account entity the server is under.
        /// </summary>
        public CloudAccount CloudAccount { get; set; }

        /// <summary>
        /// Gets or sets the cloud server tags associated to the server.
        /// </summary>
        public IEnumerable<CloudServerTag> CloudServerTags { get; set; }
    }
}