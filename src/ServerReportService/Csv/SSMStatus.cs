namespace ServerReportService.Csv
{
    public static class SSMStatus
    {
        public const string CanConnect = "Can Connect";
        public const string ServerOffline = "Server Offline";
        public const string MissingProfile = "Missing Instance Profile";
        public const string InvalidAgentVersion = "Invalid Agent Version";
        public const string NotReachable = "Agent Not Reachable";
        public const string NotManaged = "Instance Showing As Not Managed";
        public const string MissingManagedPolicy = "Instance Profile Missing AmazonSSMManagedInstanceCore Policy";
        public const string DeregisteredOrOff = "Server Deregistered or Agent Stopped/Not Installed";
        public const string Indeterminable = "All Checks Pass, But Cannot Connect";
        public const string MissingPermission = "Execution Role Lacks Permissions To Check";
        public const string NA = "N/A";
    }
}