namespace IssueReportService.Entities
{
    using System.ComponentModel.DataAnnotations;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Entity that represents a server tag from any cloud provider.
    /// </summary>
    [Index(nameof(Key), nameof(CloudServerId), IsUnique = true)]
    [Index(nameof(Key), nameof(Value), nameof(CloudServerId))]
    public class CloudServerTag : AbstractAutoIncrementWithMetadata
    {
        [Required]
        public uint CloudServerId { get; set; }

        [Required]
        public string Key { get; set; }

        [Required]
        public string Value { get; set; }

        [Required]
        public bool IsCustom { get; set; }

        /// <summary>
        /// Gets or sets the cloud server the tag is assigned to.
        /// </summary>
        public CloudServer CloudServer { get; set; }
    }
}