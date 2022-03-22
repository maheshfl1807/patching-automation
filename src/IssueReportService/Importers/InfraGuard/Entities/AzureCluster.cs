namespace IssueReportService.Importers.InfraGuard.Entities
{
    using System.Text.Json.Serialization;

    public class AzureCluster : AbstractCluster
    {
        [JsonPropertyName("tenant_id")]
        public string TenantId { get; set; }

        [JsonPropertyName("subscription_id")]
        public string SubscriptionId { get; set; }

        [JsonPropertyName("client_id")]
        public string ClientId { get; set; }

        [JsonPropertyName("client_secret")]
        public string ClientSecret { get; set; }
    }
}