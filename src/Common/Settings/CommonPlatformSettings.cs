namespace Common.Settings
{
    /// <summary>
    /// Settings related to Platform that are common across services.
    /// </summary>
    public abstract class CommonPlatformSettings
    {
        /// <summary>
        /// Gets the connection string to the Platform DB.
        /// </summary>
        public string ConnectionString { get; set; }
    }
}