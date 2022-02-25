namespace ImportService.Settings
{
    using System.Collections.Generic;
    using Common.Settings;

    /// <inheritdoc />
    public class RootSettings : CommonRootSettings
    {
        public IEnumerable<string> ValidPatchTagKeys { get; set; }

        public IEnumerable<string> InvalidPatchTagValues { get; set; }
    }
}