namespace Common.Settings
{
    /// <summary>
    /// Root level service settings that are common across services.
    /// </summary>
    public abstract class CommonRootSettings
    {
        /// <summary>
        /// The name of the service.
        /// </summary>
        public string ServiceName { get; set; }
    }
}