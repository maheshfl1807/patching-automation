// Disable requiring XML comments as all const names should be self-explanatory.
#pragma warning disable CS1591

namespace Common
{
    /// <summary>
    /// Static class to hold Platform topic names. Please follow this format when creating new:
    /// "visibility"."application"."subdomain1"."subdomain2"."..."."entityname"
    /// e.g. public.platform.entities.providers
    /// </summary>
    public class Topics
    {
        public const string PublicImportServiceCommandsImport = "public.importservice.commands.import";
        public const string PublicPlatformEntitiesAccounts = "public.platform.entities.accounts";
        public const string PublicPlatformEntitiesProviders = "public.platform.entities.providers";
    }
}