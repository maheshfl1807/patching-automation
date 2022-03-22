namespace ServerReportService.Importers.InfraGuard.Entities
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class Server
    {
        [JsonPropertyName("server_id")]
        public string ServerId { get; set; }

        [JsonPropertyName("server_name")]
        public string ServerName { get; set; }

        [JsonPropertyName("instance_id")]
        public string InstanceId { get; set; }

        [JsonPropertyName("agent_status")]
        public string AgentStatus { get; set; }

        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; }

        [JsonPropertyName("project_name")]
        public string ProjectName { get; set; }

        [JsonPropertyName("server_running")]
        public string ServerRunning { get; set; }

        public string Platform { get; set; }

        public string Provider { get; set; }

        public IEnumerable<KeyValuePair<string, string>> Tags { get; set; }

        [JsonPropertyName("server_users")]
        public IEnumerable<string> ServerUsers { get; set; }
    }
}