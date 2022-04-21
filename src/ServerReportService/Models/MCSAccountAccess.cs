namespace ServerReportService.Models
{
    using LaunchSharp.AccountAccess;
    public class MCSAccountAccess : AccountAccess
    {
        public new SourceType Source { get; set; }

        public enum SourceType
        {
            IAMGlobalRole,
            IAMRole,
            IAMUser,
            Azure,
            AzureAD,
            AzureCSP,
            MCSIAMRole,
            GCPServiceAccount,
            GCPGlobalServiceAccount,
        }
    }
}
