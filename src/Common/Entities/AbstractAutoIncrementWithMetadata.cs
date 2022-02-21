namespace Common.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// Abstract class for entities to inherit that use an auto increment id and metadata.
    /// </summary>
    public abstract class AbstractAutoIncrementWithMetadata : AbstractAutoIncrement
     {
        /// <summary>
        /// Gets or sets the DateTime that the row was created at.
        /// </summary>
        [Column(TypeName = "TIMESTAMP")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the username that the row was created by.
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the DateTime that the row was updated at.
        /// </summary>
        [Column(TypeName = "TIMESTAMP")]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the username that the row was updated by.
        /// </summary>
        public string UpdatedBy { get; set; }
    }
}