// Disable requiring XML comments as all const names should be self-explanatory.
#pragma warning disable CS1591

namespace Common
{
    /// <summary>
    /// Static class to hold Platform topic names. Please follow this format when creating new:
    /// "visibility"."application"."subdomain1"."subdomain2"."..."."entityname"."v#"
    /// e.g. public.platform.entities.providers.v1
    /// </summary>
    public class Topics
    {
        public const string PublicImportServiceCommandsImportV1 = "public.importservice.commands.import.v1";
        public const string PublicPlatformEntitiesAccountsV1 = "public.platform.entities.accounts.v1";
        public const string PublicPlatformEntitiesProvidersV1 = "public.platform.entities.providers.v1";
    }
}