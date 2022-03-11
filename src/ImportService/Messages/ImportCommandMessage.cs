namespace ImportService.Messages
{
    using System.Collections.Generic;

    // Test message: {"AccountIds":["061165946885"]}
    public class ImportCommandMessage
    {
        public IEnumerable<string> AccountIds { get; set; }

        public IEnumerable<string> ProviderNames { get; set; }

        public IEnumerable<string> ServerIds { get; set; }
    }
}