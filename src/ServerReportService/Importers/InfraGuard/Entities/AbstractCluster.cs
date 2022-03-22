namespace ServerReportService.Importers.InfraGuard.Entities
{
    using System.Text.Json.Serialization;

    public class AbstractCluster
    {
        [JsonPropertyName("cluster_id")]
        public string ClusterId { get; set; }

        public string Name { get; set; }

        [JsonPropertyName("role_arn")]
        public string RoleArn { get; set; }

        [JsonPropertyName("external_id")]
        public string ExternalId { get; set; }

        /// <summary>
        /// Gets or sets the provider. Only used on /cluster POST. Not returned by GET.
        /// </summary>
        [JsonPropertyName("provider")]
        public string Provider { get; set; }
    }
}