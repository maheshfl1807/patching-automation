namespace IssueReportService.Importers.InfraGuard.Responses
{
    using System.Collections.Generic;
    using Entities;

    public class ClusterData
    {
        public IEnumerable<AwsCluster> Aws { get; set; }

        public IEnumerable<AzureCluster> Azure { get; set; }

        public IEnumerable<GcpCluster> Gcp { get; set; }
    }
}