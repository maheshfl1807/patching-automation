namespace IssueReportService.Importers.InfraGuard.Entities
{
    using System.Text.Json.Serialization;

    public class Project
    {
        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; }

        public string Name { get; set; }

        [JsonPropertyName("assigned_servers")]
        public uint AssignedServers { get; set; }
    }
}