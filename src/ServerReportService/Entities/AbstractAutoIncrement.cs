namespace ServerReportService.Entities
{
    /// <summary>
    /// Abstract class for entities to inherit that use an auto increment id.
    /// </summary>
    public abstract class AbstractAutoIncrement
    {
        /// <summary>
        /// Gets or sets the auto increment id.
        /// </summary>
        public uint Id { get; set; }
    }
}